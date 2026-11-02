using Microsoft.EntityFrameworkCore;
using Stripe;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Payments;

public class StripeAccountUpdatedHandler : IStripeWebhookHandler
{
    private readonly IStripeGateway _stripeGateway;
    private readonly ILogger<StripeAccountUpdatedHandler> _logger;

    public StripeAccountUpdatedHandler(IStripeGateway stripeGateway, ILogger<StripeAccountUpdatedHandler> logger)
    {
        _stripeGateway = stripeGateway;
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

        var created = stripeEvent.Created;
        var lastChange = user.PayoutAccountStatusChangedAt;

        if (lastChange > created)
        {
            LogSkipped(stripeEvent, account);
            return;
        }

        if (lastChange != created)
        {
            if (await TryApplyAsync(context, user.Id, account, created, allowSameSecond: false, cancellationToken))
                return;

            var storedChange = await context.Users
                .Where(u => u.Id == user.Id)
                .Select(u => u.PayoutAccountStatusChangedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (storedChange != created)
            {
                LogSkipped(stripeEvent, account);
                return;
            }
        }

        var liveAccount = await _stripeGateway.GetAccountAsync(account.Id, cancellationToken);

        if (!await TryApplyAsync(context, user.Id, liveAccount, created, allowSameSecond: true, cancellationToken))
            LogSkipped(stripeEvent, account);
    }

    private static async Task<bool> TryApplyAsync(
        ApplicationDbContext context,
        int userId,
        Account account,
        DateTime created,
        bool allowSameSecond,
        CancellationToken cancellationToken)
    {
        var accountId = account.Id;
        var status = PayoutAccountStatusMapper.FromAccount(account);

        var target = context.Users.Where(u => u.Id == userId && (u.StripeAccountId == null || u.StripeAccountId == accountId));

        target = allowSameSecond
            ? target.Where(u => u.PayoutAccountStatusChangedAt == null || u.PayoutAccountStatusChangedAt <= created)
            : target.Where(u => u.PayoutAccountStatusChangedAt == null || u.PayoutAccountStatusChangedAt < created);

        var updated = await target.ExecuteUpdateAsync(s => s
            .SetProperty(u => u.StripeAccountId, accountId)
            .SetProperty(u => u.PayoutAccountStatus, status)
            .SetProperty(u => u.PayoutAccountStatusChangedAt, created),
            cancellationToken);

        return updated > 0;
    }

    private void LogSkipped(Event stripeEvent, Account account) =>
        _logger.LogInformation(
            "Stripe event {EventId} for account {AccountId} is older than the last status change and was skipped",
            stripeEvent.Id,
            account.Id);

    private static async Task<ApplicationUser?> FindUserAsync(Account account, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.StripeAccountId == account.Id, cancellationToken);
        if (user is not null)
            return user;

        if (account.Metadata is null
            || !account.Metadata.TryGetValue(StripeGateway.UserIdMetadataKey, out var rawUserId)
            || !int.TryParse(rawUserId, out var userId))
            return null;

        return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.StripeAccountId == null, cancellationToken);
    }
}
