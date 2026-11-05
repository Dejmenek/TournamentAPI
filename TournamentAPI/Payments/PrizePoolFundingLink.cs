using Stripe.Checkout;

namespace TournamentAPI.Payments;

public record PrizePoolFundingLink(string Url, DateTime? ExpiresAt)
{
    internal static PrizePoolFundingLink From(Session session) => new(session.Url, session.ExpiresAt);
}
