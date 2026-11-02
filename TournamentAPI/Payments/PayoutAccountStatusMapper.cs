using Stripe;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Payments;

public static class PayoutAccountStatusMapper
{
    private const string ActiveCapability = "active";
    private const string PendingCapability = "pending";

    public static PayoutAccountStatus FromAccount(Account account)
    {
        var transfers = account.Capabilities?.Transfers;

        if (transfers == ActiveCapability)
            return PayoutAccountStatus.Enabled;

        if (!account.DetailsSubmitted || transfers == PendingCapability || IsOnlyAwaitingVerification(account.Requirements))
            return PayoutAccountStatus.Pending;

        return PayoutAccountStatus.Restricted;
    }

    private static bool IsOnlyAwaitingVerification(AccountRequirements? requirements) =>
        requirements is not null
        && requirements.PendingVerification is { Count: > 0 }
        && requirements.CurrentlyDue is null or { Count: 0 }
        && requirements.PastDue is null or { Count: 0 };
}
