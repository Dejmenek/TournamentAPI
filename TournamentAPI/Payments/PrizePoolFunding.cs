using Stripe;
using Stripe.Checkout;

namespace TournamentAPI.Payments;

public static class PrizePoolFunding
{
    public const string SessionTypeMetadataKey = "type";
    public const string SessionTypeValue = "prize_pool_funding";
    public const string TournamentIdMetadataKey = "tournament_id";
    public const string OwnerIdMetadataKey = "owner_id";

    public const string CheckoutModePayment = "payment";
    public const string CardPaymentMethod = "card";
    public const string SessionStatusOpen = "open";
    public const string SessionStatusComplete = "complete";

    public const int StripeMinimumExpiryMinutes = 30;
    public const int ExpiryMarginMinutes = 1;

    public static string TransferGroup(int tournamentId) => $"tournament-{tournamentId}";

    public static string IdempotencyKey(int tournamentId, int attempt) => $"prize-pool-tournament-{tournamentId}-{attempt}";

    internal static bool TryGetFundingSession(Event stripeEvent, ILogger logger, out Session session)
    {
        if (stripeEvent.Data.Object is not Session payload)
        {
            logger.LogWarning("Stripe event {EventId} of type {EventType} has no session payload", stripeEvent.Id, stripeEvent.Type);
            session = null!;
            return false;
        }

        session = payload;

        return payload.Metadata is not null
               && payload.Metadata.TryGetValue(SessionTypeMetadataKey, out var type)
               && type == SessionTypeValue;
    }
}
