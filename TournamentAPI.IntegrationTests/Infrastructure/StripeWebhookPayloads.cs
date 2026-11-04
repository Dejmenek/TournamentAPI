using System.Net.Http;
using System.Text;
using System.Text.Json;
using Stripe;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed record SignedStripeEvent(string EventId, string Payload, string SignatureHeader);

public static class StripeWebhookPayloads
{
    public const string TestSigningSecret = "whsec_integration_test_signing_secret";

    public static SignedStripeEvent Create(
        string eventType,
        string? eventId = null,
        object? dataObject = null,
        string? apiVersion = null,
        DateTimeOffset? created = null)
    {
        var id = eventId ?? $"evt_test_{Guid.NewGuid():N}";

        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["object"] = "event",
            ["api_version"] = apiVersion ?? StripeConfiguration.ApiVersion,
            ["created"] = (created ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds(),
            ["livemode"] = false,
            ["pending_webhooks"] = 1,
            ["type"] = eventType,
            ["data"] = new Dictionary<string, object?>
            {
                ["object"] = dataObject ?? new Dictionary<string, object?>
                {
                    ["id"] = "ch_test_default",
                    ["object"] = "charge"
                }
            }
        });

        var signatureHeader = EventUtility.GenerateSignatureHeader(payload, TestSigningSecret);

        return new SignedStripeEvent(id, payload, signatureHeader);
    }

    public static SignedStripeEvent AccountUpdated(
        string accountId,
        string transfers,
        bool detailsSubmitted,
        DateTimeOffset? created = null,
        string[]? outstandingRequirements = null,
        string? disabledReason = null,
        int? metadataUserId = null) =>
        Create(
            "account.updated",
            created: created,
            dataObject: new Dictionary<string, object?>
            {
                ["id"] = accountId,
                ["object"] = "account",
                ["details_submitted"] = detailsSubmitted,
                ["capabilities"] = new Dictionary<string, object?> { ["transfers"] = transfers },
                ["requirements"] = new Dictionary<string, object?>
                {
                    ["currently_due"] = outstandingRequirements ?? [],
                    ["past_due"] = outstandingRequirements ?? [],
                    ["disabled_reason"] = disabledReason
                },
                ["metadata"] = metadataUserId is null
                    ? new Dictionary<string, object?>()
                    : new Dictionary<string, object?> { [StripeGateway.UserIdMetadataKey] = metadataUserId.Value.ToString() }
            });

    public static HttpRequestMessage ToRequest(this SignedStripeEvent signedEvent, string? signatureHeader = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/stripe")
        {
            Content = new StringContent(signedEvent.Payload, Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("Stripe-Signature", signatureHeader ?? signedEvent.SignatureHeader);

        return request;
    }
}
