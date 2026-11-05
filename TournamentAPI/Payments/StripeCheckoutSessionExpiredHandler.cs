using Microsoft.EntityFrameworkCore;
using Stripe;
using TournamentAPI.Data;

namespace TournamentAPI.Payments;

public class StripeCheckoutSessionExpiredHandler : IStripeWebhookHandler
{
    private readonly ILogger<StripeCheckoutSessionExpiredHandler> _logger;

    public StripeCheckoutSessionExpiredHandler(ILogger<StripeCheckoutSessionExpiredHandler> logger)
    {
        _logger = logger;
    }

    public string EventType => EventTypes.CheckoutSessionExpired;

    public async Task HandleAsync(Event stripeEvent, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (!PrizePoolFunding.TryGetFundingSession(stripeEvent, _logger, out var session))
            return;

        var cleared = await PrizePoolFundingSessions.ResetAsync(context, session.Id, cancellationToken);

        if (cleared == 0)
            _logger.LogInformation("Expired funding session {SessionId} is no longer the stored session and was ignored", session.Id);
    }
}
