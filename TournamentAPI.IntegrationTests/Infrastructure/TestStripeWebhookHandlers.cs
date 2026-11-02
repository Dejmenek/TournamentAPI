using System.Collections.Concurrent;
using Stripe;
using TournamentAPI.Data;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed class RecordingStripeWebhookHandler : IStripeWebhookHandler
{
    public const string HandledEventType = "tournamentapi_test.recorded";

    private readonly ConcurrentQueue<string> _handledEventIds = new();

    public string EventType => HandledEventType;

    public IReadOnlyList<string> HandledEventIds => _handledEventIds.ToArray();

    public Task HandleAsync(Event stripeEvent, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _handledEventIds.Enqueue(stripeEvent.Id);
        return Task.CompletedTask;
    }

    public void Reset() => _handledEventIds.Clear();
}

public sealed class ThrowingStripeWebhookHandler : IStripeWebhookHandler
{
    public const string HandledEventType = "tournamentapi_test.failing";

    public string EventType => HandledEventType;

    public Task HandleAsync(Event stripeEvent, ApplicationDbContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Handler failure triggered by test.");
}
