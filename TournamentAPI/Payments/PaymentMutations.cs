using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Data.Common;
using System.Net;
using System.Security.Claims;
using TournamentAPI.Configuration;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Tournaments;
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

    [Authorize]
    public static async Task<PrizePoolFundingLink?> FundTournamentPrizePool(
        FundTournamentPrizePoolInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IStripeGateway stripeGateway,
        IOptions<StripeOptions> stripeOptions,
        IOptions<PaidTournamentOptions> paidTournamentOptions,
        IResolverContext resolverContext,
        ILoggerFactory loggerFactory,
        CancellationToken token)
    {
        var logger = loggerFactory.CreateLogger(typeof(PaymentMutations).FullName!);
        var userId = userClaims.GetUserId();

        using var _ = resolverContext.PushEntityContext("Tournament", input.TournamentId);

        var tournament = await context.Tournaments
            .AsNoTracking()
            .Include(t => t.Prizes)
            .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, input.TournamentId)))
            return null;

        if (tournament is null)
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament.OwnerId, userId, tournament.Id)))
            return null;

        if (resolverContext.TryReportError(PaymentValidations.ValidatePrizePoolFundable(tournament, DateTime.UtcNow)))
            return null;

        if (tournament.EntryFeeAmount > 0)
        {
            var payoutStatus = await context.Users
                .Where(u => u.Id == tournament.OwnerId)
                .Select(u => u.PayoutAccountStatus)
                .FirstAsync(token);

            if (resolverContext.TryReportError(PaymentValidations.ValidatePayoutAccountEnabledForPaidTournament(tournament.OwnerId, tournament.EntryFeeAmount, payoutStatus)))
                return null;
        }

        var funding = new FundingScope(
            context, stripeGateway, stripeOptions.Value, paidTournamentOptions.Value, resolverContext, logger, token);

        try
        {
            var stored = await CheckStoredFundingSessionAsync(funding, tournament);
            if (stored.IsResolved)
                return stored.Link;

            return await CreateAndStoreFundingSessionAsync(funding, tournament, stored.Attempt);
        }
        catch (Exception ex) when (IsStripeFailure(ex, token))
        {
            logger.LogError(ex, "Funding the prize pool failed for tournament {TournamentId}", tournament.Id);
            resolverContext.ReportError(PaymentErrors.StripeRequestFailed());
            return null;
        }
    }

    private static async Task<StoredSessionCheck> CheckStoredFundingSessionAsync(FundingScope funding, Tournament tournament)
    {
        if (tournament.FundingSessionId is not { } storedSessionId)
            return StoredSessionCheck.Continue(tournament.FundingSessionAttempt);

        var stored = await funding.Gateway.GetCheckoutSessionAsync(storedSessionId, funding.Token);

        if (stored.Status == PrizePoolFunding.SessionStatusOpen)
            return StoredSessionCheck.Resolved(PrizePoolFundingLink.From(stored));

        if (stored.Status == PrizePoolFunding.SessionStatusComplete)
            return StoredSessionCheck.Resolved(ReportNotFundable(funding, tournament.Id));

        await PrizePoolFundingSessions.ResetAsync(funding.Context, storedSessionId, funding.Token);

        var current = await PrizePoolFundingSessions.ReadAsync(funding.Context, tournament.Id, funding.Token);

        if (current is null)
            return StoredSessionCheck.Resolved(ReportNotFundable(funding, tournament.Id));

        if (current.SessionId is { } winnerId)
            return StoredSessionCheck.Resolved(await ReadSessionLinkAsync(funding, winnerId));

        return StoredSessionCheck.Continue(current.Attempt);
    }

    private static async Task<PrizePoolFundingLink?> CreateAndStoreFundingSessionAsync(
        FundingScope funding,
        Tournament tournament,
        int attempt)
    {
        var prizeTotal = tournament.Prizes.Sum(p => p.Amount);

        var expiresAt = await PrizePoolFundingSessions.ReserveExpiryAsync(
            funding.Context, tournament.Id, funding.PaidOptions.CheckoutExpiryMinutes, DateTime.UtcNow, funding.Token);

        if (expiresAt is null)
            return ReportNotFundable(funding, tournament.Id);

        var session = await CreateCheckoutSessionAsync(funding, tournament, attempt, expiresAt.Value);

        if (session is null)
            return await ReadWinnerOrReportFailureAsync(funding, tournament.Id);

        var saved = await PrizePoolFundingSessions.FillAsync(
            funding.Context, tournament, prizeTotal, session, DateTime.UtcNow, funding.Token);

        if (saved > 0)
            return PrizePoolFundingLink.From(session);

        var winner = (await PrizePoolFundingSessions.ReadAsync(funding.Context, tournament.Id, funding.Token))?.SessionId;

        if (winner is null)
        {
            funding.Logger.LogWarning(
                "Checkout session {SessionId} for tournament {TournamentId} was not stored because the tournament changed while it was created",
                session.Id, tournament.Id);

            await AdvanceFundingAttemptAsync(funding, tournament.Id, attempt);
            return ReportNotFundable(funding, tournament.Id);
        }

        if (winner == session.Id)
            return PrizePoolFundingLink.From(session);

        funding.Logger.LogWarning(
            "Checkout session {SessionId} for tournament {TournamentId} lost the race to session {WinnerSessionId}",
            session.Id, tournament.Id, winner);

        return await ReadSessionLinkAsync(funding, winner);
    }

    private static async Task<Session?> CreateCheckoutSessionAsync(
        FundingScope funding,
        Tournament tournament,
        int attempt,
        DateTime expiresAt)
    {
        var request = new CheckoutSessionRequest(
            PrizePoolFunding.IdempotencyKey(tournament.Id, attempt),
            PaidTournamentFeeCalculator.CalculateEscrowAmount(
                tournament.Prizes.Select(p => p.Amount),
                funding.PaidOptions.PlatformFeePercent),
            funding.StripeOptions.Currency,
            $"Prize pool for {tournament.Name}",
            funding.PaidOptions.FundingSuccessUrl,
            funding.PaidOptions.FundingCancelUrl,
            PrizePoolFunding.TransferGroup(tournament.Id),
            new Dictionary<string, string>
            {
                [PrizePoolFunding.SessionTypeMetadataKey] = PrizePoolFunding.SessionTypeValue,
                [PrizePoolFunding.TournamentIdMetadataKey] = tournament.Id.ToString(),
                [PrizePoolFunding.OwnerIdMetadataKey] = tournament.OwnerId.ToString()
            },
            expiresAt);

        try
        {
            return await funding.Gateway.CreateCheckoutSessionAsync(request, funding.Token);
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }
        catch (StripeException)
        {
            await AdvanceFundingAttemptAsync(funding, tournament.Id, attempt);
            throw;
        }
    }

    private static async Task AdvanceFundingAttemptAsync(FundingScope funding, int tournamentId, int attempt)
    {
        try
        {
            await PrizePoolFundingSessions.AdvanceAttemptAsync(funding.Context, tournamentId, attempt, funding.Token);
        }
        catch (DbException ex)
        {
            funding.Logger.LogError(ex, "Advancing the funding attempt failed for tournament {TournamentId}", tournamentId);
        }
    }

    private static async Task<PrizePoolFundingLink?> ReadWinnerOrReportFailureAsync(FundingScope funding, int tournamentId)
    {
        var current = await PrizePoolFundingSessions.ReadAsync(funding.Context, tournamentId, funding.Token);

        if (current?.SessionId is { } winnerId)
            return await ReadSessionLinkAsync(funding, winnerId);

        funding.Logger.LogWarning(
            "Stripe reported a funding request for tournament {TournamentId} still in flight and no session is stored yet",
            tournamentId);
        funding.Resolver.ReportError(PaymentErrors.StripeRequestFailed());
        return null;
    }

    private static async Task<PrizePoolFundingLink> ReadSessionLinkAsync(FundingScope funding, string sessionId)
        => PrizePoolFundingLink.From(await funding.Gateway.GetCheckoutSessionAsync(sessionId, funding.Token));

    private static PrizePoolFundingLink? ReportNotFundable(FundingScope funding, int tournamentId)
    {
        funding.Resolver.ReportError(PaymentErrors.PrizePoolNotFundable(tournamentId));
        return null;
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

    private sealed record FundingScope(
        ApplicationDbContext Context,
        IStripeGateway Gateway,
        StripeOptions StripeOptions,
        PaidTournamentOptions PaidOptions,
        IResolverContext Resolver,
        ILogger Logger,
        CancellationToken Token);

    private sealed record StoredSessionCheck(bool IsResolved, PrizePoolFundingLink? Link, int Attempt)
    {
        public static StoredSessionCheck Resolved(PrizePoolFundingLink? link) => new(true, link, 0);

        public static StoredSessionCheck Continue(int attempt) => new(false, null, attempt);
    }
}
