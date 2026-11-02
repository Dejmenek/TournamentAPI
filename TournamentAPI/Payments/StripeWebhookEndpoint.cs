using Microsoft.Extensions.Options;
using Stripe;
using TournamentAPI.Configuration;

namespace TournamentAPI.Payments;

internal static class StripeWebhookEndpoint
{
    internal const string Path = "/webhooks/stripe";
    internal const string SignatureHeaderName = "Stripe-Signature";
    private const string LoggerCategory = "TournamentAPI.Payments.StripeWebhookEndpoint";

    internal static IEndpointRouteBuilder MapStripeWebhook(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(Path, HandleAsync)
            .AllowAnonymous()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        StripeWebhookProcessor processor,
        IOptions<StripeOptions> stripeOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(LoggerCategory);

        var signatureHeader = request.Headers[SignatureHeaderName].ToString();
        if (string.IsNullOrEmpty(signatureHeader))
        {
            logger.LogWarning("Stripe webhook rejected: missing {Header} header", SignatureHeaderName);
            return Results.BadRequest();
        }

        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                stripeOptions.Value.WebhookSigningSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook rejected: signature or payload could not be verified");
            return Results.BadRequest();
        }

        try
        {
            var outcome = await processor.ProcessAsync(stripeEvent, cancellationToken);

            logger.LogInformation(
                "Stripe event {EventId} of type {EventType} finished as {Outcome}",
                stripeEvent.Id,
                stripeEvent.Type,
                outcome);

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Stripe event {EventId} of type {EventType} failed; Stripe will retry",
                stripeEvent.Id,
                stripeEvent.Type);

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
