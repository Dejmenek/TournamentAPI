using Stripe.Checkout;

namespace TournamentAPI.Payments;

public interface IStripeGateway
{
    Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken);
}
