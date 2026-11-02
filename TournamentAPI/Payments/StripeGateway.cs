using Stripe;
using Stripe.Checkout;

namespace TournamentAPI.Payments;

public class StripeGateway : IStripeGateway
{
    private readonly StripeClient _client;

    public StripeGateway(StripeClient client)
    {
        _client = client;
    }

    public Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken)
        => _client.V1.Checkout.Sessions.GetAsync(sessionId, cancellationToken: cancellationToken);
}
