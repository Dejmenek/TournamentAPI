using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Payments;

public class StripeAccountUpdatedHandler : IStripeWebhookHandler
{
    private readonly ILogger<StripeAccountUpdatedHandler> _logger;

    public StripeAccountUpdatedHandler(ILogger<StripeAccountUpdatedHandler> logger)
    {
        _logger = logger;
    }

    public string EventType => EventTypes.AccountUpdated;

    public async Task HandleAsync(Event stripeEvent, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Account account)
        {
            _logger.LogWarning("Stripe event {EventId} of type {EventType} has no account payload", stripeEvent.Id, stripeEvent.Type);
            return;
        }

        var user = await FindUserAsync(account, context, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Stripe event {EventId} refers to account {AccountId}, which belongs to no user", stripeEvent.Id, account.Id);
            return;
        }

        if (user.PayoutAccountStatusChangedAt is { } lastChange && stripeEvent.Created < lastChange)
        {
            _logger.LogInformation(
                "Stripe event {EventId} for account {AccountId} is older than the last status change and was skipped",
                stripeEvent.Id,
                account.Id);
            return;
        }

        user.StripeAccountId ??= account.Id;
        user.PayoutAccountStatus = PayoutAccountStatusMapper.FromAccount(account);
        user.PayoutAccountStatusChangedAt = stripeEvent.Created;
    }

    private static async Task<ApplicationUser?> FindUserAsync(Account account, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.StripeAccountId == account.Id, cancellationToken);
        if (user is not null)
            return user;

        if (account.Metadata is null
            || !account.Metadata.TryGetValue(StripeGateway.UserIdMetadataKey, out var rawUserId)
            || !int.TryParse(rawUserId, out var userId))
            return null;

        return await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.StripeAccountId == null, cancellationToken);
    }
}
