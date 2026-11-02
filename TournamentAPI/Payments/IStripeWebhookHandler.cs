using Stripe;
using TournamentAPI.Data;

namespace TournamentAPI.Payments;

/// <summary>
/// Handles one Stripe event type. Changes go through the given context without calling
/// SaveChanges, so they commit in the same transaction as the ProcessedStripeEvents row.
/// Handlers must check the current state before changing it, so a replayed or out-of-order event does nothing.
/// </summary>
public interface IStripeWebhookHandler
{
    string EventType { get; }

    Task HandleAsync(Event stripeEvent, ApplicationDbContext context, CancellationToken cancellationToken);
}
