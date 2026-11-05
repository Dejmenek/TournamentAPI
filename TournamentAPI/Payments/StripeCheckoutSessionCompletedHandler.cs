using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Payments;

public class StripeCheckoutSessionCompletedHandler : IStripeWebhookHandler
{
    private readonly IStripeGateway _stripeGateway;
    private readonly ILogger<StripeCheckoutSessionCompletedHandler> _logger;

    public StripeCheckoutSessionCompletedHandler(IStripeGateway stripeGateway, ILogger<StripeCheckoutSessionCompletedHandler> logger)
    {
        _stripeGateway = stripeGateway;
        _logger = logger;
    }

    public string EventType => EventTypes.CheckoutSessionCompleted;

    public async Task HandleAsync(Event stripeEvent, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (!PrizePoolFunding.TryGetFundingSession(stripeEvent, _logger, out var session))
            return;

        var matchesStoredSession = await context.Tournaments
            .AnyAsync(t => t.FundingSessionId == session.Id && t.PrizePoolStatus == PrizePoolStatus.Unfunded, cancellationToken);

        if (!matchesStoredSession)
        {
            LogUnmatchedPayment(stripeEvent, session);
            return;
        }

        if (string.IsNullOrEmpty(session.PaymentIntentId))
        {
            _logger.LogError(
                "Stripe event {EventId} reports a paid prize pool funding session {SessionId} without a payment intent; the tournament was not marked funded",
                stripeEvent.Id,
                session.Id);
            return;
        }

        var paymentIntent = await _stripeGateway.GetPaymentIntentAsync(session.PaymentIntentId, cancellationToken);

        var funded = await context.Tournaments
            .Where(t => t.FundingSessionId == session.Id && t.PrizePoolStatus == PrizePoolStatus.Unfunded)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.PrizePoolStatus, PrizePoolStatus.Funded)
                .SetProperty(t => t.FundingChargeId, paymentIntent.LatestChargeId),
                cancellationToken);

        if (funded == 0)
            LogUnmatchedPayment(stripeEvent, session);
    }

    private void LogUnmatchedPayment(Event stripeEvent, Session session) =>
        _logger.LogError(
            "Stripe event {EventId} reports a paid prize pool funding session {SessionId} that matches no unfunded tournament; the payment is not refunded",
            stripeEvent.Id,
            session.Id);
}
