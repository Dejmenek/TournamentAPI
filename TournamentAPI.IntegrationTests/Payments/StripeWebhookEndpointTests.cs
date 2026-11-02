using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using TournamentAPI.IntegrationTests.Infrastructure;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests.Payments;

public class StripeWebhookEndpointTests : BaseIntegrationTest
{
    public StripeWebhookEndpointTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SignedEvent_Returns200_AndRecordsProcessedEvent()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create(RecordingStripeWebhookHandler.HandledEventType);

        // Act
        var response = await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([signedEvent.EventId], Factory.RecordingWebhookHandler.HandledEventIds);

        var processedEvent = await DbContext.ProcessedStripeEvents.AsNoTracking().SingleAsync();
        Assert.Equal(signedEvent.EventId, processedEvent.Id);
        Assert.Equal(RecordingStripeWebhookHandler.HandledEventType, processedEvent.Type);
    }

    [Fact]
    public async Task SameEventDeliveredTwice_Returns200BothTimes_AndRunsHandlerOnce()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create(RecordingStripeWebhookHandler.HandledEventType);

        // Act
        var firstResponse = await client.SendAsync(signedEvent.ToRequest());
        var secondResponse = await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal([signedEvent.EventId], Factory.RecordingWebhookHandler.HandledEventIds);
        Assert.Equal(1, await DbContext.ProcessedStripeEvents.CountAsync());
    }

    [Fact]
    public async Task MissingSignature_Returns400_AndWritesNothing()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create(RecordingStripeWebhookHandler.HandledEventType);
        using var request = signedEvent.ToRequest();
        request.Headers.Remove("Stripe-Signature");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(Factory.RecordingWebhookHandler.HandledEventIds);
        Assert.False(await DbContext.ProcessedStripeEvents.AnyAsync());
    }

    [Fact]
    public async Task InvalidSignature_Returns400_AndWritesNothing()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create(RecordingStripeWebhookHandler.HandledEventType);
        var forgedSignature = EventUtility.GenerateSignatureHeader(signedEvent.Payload, "whsec_wrong_secret");

        // Act
        var response = await client.SendAsync(signedEvent.ToRequest(forgedSignature));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(Factory.RecordingWebhookHandler.HandledEventIds);
        Assert.False(await DbContext.ProcessedStripeEvents.AnyAsync());
    }

    [Fact]
    public async Task TamperedPayload_Returns400_AndWritesNothing()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create(RecordingStripeWebhookHandler.HandledEventType);
        var tamperedEvent = signedEvent with { Payload = signedEvent.Payload.Replace("ch_test_default", "ch_test_tampered") };

        // Act
        var response = await client.SendAsync(tamperedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await DbContext.ProcessedStripeEvents.AnyAsync());
    }

    [Fact]
    public async Task SignedEventFromDifferentApiVersion_Returns200_AndIsProcessed()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create(
            RecordingStripeWebhookHandler.HandledEventType,
            apiVersion: "2020-08-27");

        // Act
        var response = await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([signedEvent.EventId], Factory.RecordingWebhookHandler.HandledEventIds);
    }

    [Fact]
    public async Task HandlerThrows_Returns500_AndKeepsNoProcessedEvent()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create(ThrowingStripeWebhookHandler.HandledEventType);

        // Act
        var response = await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.False(await DbContext.ProcessedStripeEvents.AnyAsync());
    }

    [Fact]
    public async Task UnknownEventType_Returns200_AndIsIgnored()
    {
        // Arrange
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.Create("customer.created");

        // Act
        var response = await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Factory.RecordingWebhookHandler.HandledEventIds);
        Assert.False(await DbContext.ProcessedStripeEvents.AnyAsync());
    }

    [Fact]
    public void StripeGateway_IsReplacedByRecordingFake()
    {
        // Act
        var gateway = Factory.Services.GetRequiredService<IStripeGateway>();

        // Assert
        Assert.Same(Factory.StripeGateway, gateway);
    }
}
