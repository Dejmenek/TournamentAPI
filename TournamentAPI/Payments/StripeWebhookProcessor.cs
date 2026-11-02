using Microsoft.EntityFrameworkCore;
using Stripe;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;

namespace TournamentAPI.Payments;

public enum StripeWebhookOutcome
{
    Processed,
    Duplicate,
    Ignored
}

public class StripeWebhookProcessor
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IReadOnlyDictionary<string, IStripeWebhookHandler> _handlers;

    public StripeWebhookProcessor(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IEnumerable<IStripeWebhookHandler> handlers)
    {
        _contextFactory = contextFactory;
        _handlers = handlers.ToDictionary(h => h.EventType, StringComparer.Ordinal);
    }

    public async Task<StripeWebhookOutcome> ProcessAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(stripeEvent.Type, out var handler))
            return StripeWebhookOutcome.Ignored;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.ProcessedStripeEvents.Add(new ProcessedStripeEvent
        {
            Id = stripeEvent.Id,
            Type = stripeEvent.Type,
            ProcessedAt = DateTime.UtcNow
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation(ProcessedStripeEvent.PrimaryKeyName))
        {
            return StripeWebhookOutcome.Duplicate;
        }

        await handler.HandleAsync(stripeEvent, context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return StripeWebhookOutcome.Processed;
    }
}
