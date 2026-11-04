using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using TournamentAPI.Configuration;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Users;

namespace TournamentAPI.Payments;

[MutationType]
public static partial class PaymentMutations
{
    [Authorize]
    public static async Task<StripeOnboardingLink?> StartStripeOnboarding(
        StartStripeOnboardingInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IStripeGateway stripeGateway,
        IOptions<StripeOptions> stripeOptions,
        IResolverContext resolverContext,
        ILoggerFactory loggerFactory,
        CancellationToken token)
    {
        var logger = loggerFactory.CreateLogger(typeof(PaymentMutations).FullName!);
        var userId = userClaims.GetUserId();

        using var _ = resolverContext.PushEntityContext("User", userId);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, token);
        if (resolverContext.TryReportError(UserValidations.ValidateUserExists(user, userId)))
            return null;

        var accountId = user!.StripeAccountId;
        if (accountId is null)
        {
            var country = input.Country.Trim().ToUpperInvariant();
            if (resolverContext.TryReportError(PaymentValidations.ValidatePayoutCountry(country)))
                return null;

            Account account;
            try
            {
                account = await stripeGateway.CreateConnectedAccountAsync(
                    new ConnectedAccountRequest(user.Id, country, user.Email, user.StripeAccountCreationAttempt),
                    token);
            }
            catch (Exception ex) when (IsStripeFailure(ex, token))
            {
                if (ex is StripeException { HttpStatusCode: not HttpStatusCode.Conflict })
                    await AdvanceAccountCreationAttemptAsync(context, user, logger, token);

                return ReportStripeFailure(resolverContext, logger, ex, "Creating a Stripe connected account", userId);
            }

            try
            {
                accountId = await SaveConnectedAccountAsync(context, user.Id, account.Id, logger, token);
            }
            catch (DbException ex)
            {
                logger.LogError(ex, "Saving Stripe account {StripeAccountId} failed for user {UserId}", account.Id, userId);
            }

            if (accountId is null)
            {
                resolverContext.ReportError(PaymentErrors.PayoutAccountSaveFailed());
                return null;
            }
        }

        try
        {
            var options = stripeOptions.Value;
            var link = await stripeGateway.CreateOnboardingLinkAsync(
                new OnboardingLinkRequest(accountId, options.OnboardingRefreshUrl, options.OnboardingReturnUrl),
                token);

            return new StripeOnboardingLink(link.Url, link.ExpiresAt);
        }
        catch (Exception ex) when (IsStripeFailure(ex, token))
        {
            return ReportStripeFailure(resolverContext, logger, ex, "Creating a Stripe onboarding link", userId);
        }
    }

    private static bool IsStripeFailure(Exception ex, CancellationToken token) =>
        ex is StripeException or HttpRequestException
        || (ex is OperationCanceledException && !token.IsCancellationRequested);

    private static StripeOnboardingLink? ReportStripeFailure(
        IResolverContext resolverContext,
        ILogger logger,
        Exception ex,
        string operation,
        int userId)
    {
        logger.LogError(ex, "{Operation} failed for user {UserId}", operation, userId);
        resolverContext.ReportError(PaymentErrors.StripeRequestFailed());
        return null;
    }

    private static async Task<string?> SaveConnectedAccountAsync(
        ApplicationDbContext context,
        int userId,
        string createdAccountId,
        ILogger logger,
        CancellationToken token)
    {
        var updated = await context.Users
            .Where(u => u.Id == userId && u.StripeAccountId == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.StripeAccountId, createdAccountId)
                .SetProperty(u => u.PayoutAccountStatus, PayoutAccountStatus.Pending),
                token);

        if (updated > 0)
            return createdAccountId;

        var storedAccountId = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.StripeAccountId)
            .FirstOrDefaultAsync(token);

        if (storedAccountId is null)
        {
            logger.LogWarning(
                "User {UserId} was not found when saving Stripe account {CreatedAccountId}; the account is not linked",
                userId, createdAccountId);
        }
        else if (storedAccountId != createdAccountId)
        {
            logger.LogWarning(
                "User {UserId} already has Stripe account {StoredAccountId}; account {CreatedAccountId} was created but is not linked",
                userId, storedAccountId, createdAccountId);
        }

        return storedAccountId;
    }

    private static async Task AdvanceAccountCreationAttemptAsync(
        ApplicationDbContext context,
        ApplicationUser user,
        ILogger logger,
        CancellationToken token)
    {
        var attempt = user.StripeAccountCreationAttempt;

        try
        {
            await context.Users
                .Where(u => u.Id == user.Id && u.StripeAccountId == null && u.StripeAccountCreationAttempt == attempt)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.StripeAccountCreationAttempt, attempt + 1), token);
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Advancing the Stripe account creation attempt failed for user {UserId}", user.Id);
        }
    }
}
