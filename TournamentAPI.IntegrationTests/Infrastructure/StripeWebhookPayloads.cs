using System.Net.Http;
using System.Text;
using System.Text.Json;
using Stripe;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed record SignedStripeEvent(string EventId, string Payload, string SignatureHeader);

public static class StripeWebhookPayloads
{
    public const string TestSigningSecret = "whsec_integration_test_signing_secret";

    public static SignedStripeEvent Create(
        string eventType,
        string? eventId = null,
        object? dataObject = null,
        string? apiVersion = null)
    {
        var id = eventId ?? $"evt_test_{Guid.NewGuid():N}";

        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["object"] = "event",
            ["api_version"] = apiVersion ?? StripeConfiguration.ApiVersion,
            ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
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
