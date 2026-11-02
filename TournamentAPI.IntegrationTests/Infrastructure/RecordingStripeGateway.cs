using System.Collections.Concurrent;
using Stripe.Checkout;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed record StripeGatewayCall(string Method, object Arguments);

public sealed class RecordingStripeGateway : IStripeGateway
{
    private readonly ConcurrentQueue<StripeGatewayCall> _calls = new();

    public IReadOnlyList<StripeGatewayCall> Calls => _calls.ToArray();

    public Func<string, Session> CheckoutSessionResult { get; set; } = DefaultCheckoutSession;

    public Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new StripeGatewayCall(nameof(GetCheckoutSessionAsync), sessionId));
        return Task.FromResult(CheckoutSessionResult(sessionId));
    }

    public void Reset()
    {
        _calls.Clear();
        CheckoutSessionResult = DefaultCheckoutSession;
    }

    private static Session DefaultCheckoutSession(string sessionId) => new() { Id = sessionId, Object = "checkout.session" };
}
