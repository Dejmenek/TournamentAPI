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

        if (!account.DetailsSubmitted || transfers == PendingCapability)
            return PayoutAccountStatus.Pending;

        return PayoutAccountStatus.Restricted;
    }
}
