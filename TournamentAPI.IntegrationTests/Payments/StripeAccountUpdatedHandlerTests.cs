using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Net;
using TournamentAPI.Data.Models;
using TournamentAPI.IntegrationTests.Infrastructure;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests.Payments;

public class StripeAccountUpdatedHandlerTests : BaseIntegrationTest
{
    private const string AccountId = "acct_test_alice";

    public StripeAccountUpdatedHandlerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AccountUpdated_WithActiveTransfers_SetsStatusToEnabled()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.AccountUpdated(AccountId, transfers: "active", detailsSubmitted: true);

        // Act
        var response = await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PayoutAccountStatus.Enabled, await GetStatusAsync(aliceId));
    }

    [Fact]
    public async Task AccountUpdated_WithDisabledTransfersAfterOnboarding_SetsStatusToRestricted()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.AccountUpdated(
            AccountId,
            transfers: "inactive",
            detailsSubmitted: true,
            outstandingRequirements: ["individual.verification.document"],
            disabledReason: "requirements.past_due");

        // Act
        await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(PayoutAccountStatus.Restricted, await GetStatusAsync(aliceId));
    }

    [Fact]
    public async Task AccountUpdated_WithOutstandingOnboardingRequirements_KeepsStatusPending()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.AccountUpdated(
            AccountId,
            transfers: "inactive",
            detailsSubmitted: false,
            outstandingRequirements: ["individual.first_name"],
            disabledReason: "requirements.past_due");

        // Act
        await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(PayoutAccountStatus.Pending, await GetStatusAsync(aliceId));
    }

    [Fact]
    public async Task AccountUpdated_OlderEventReplayedAfterNewerOne_DoesNotMoveStatusBackwards()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var now = DateTimeOffset.UtcNow;
        var olderEvent = StripeWebhookPayloads.AccountUpdated(
            AccountId,
            transfers: "pending",
            detailsSubmitted: true,
            created: now.AddMinutes(-5));
        var newerEvent = StripeWebhookPayloads.AccountUpdated(
            AccountId,
            transfers: "active",
            detailsSubmitted: true,
            created: now);

        // Act
        var newerResponse = await client.SendAsync(newerEvent.ToRequest());
        var olderResponse = await client.SendAsync(olderEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, newerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, olderResponse.StatusCode);
        Assert.Equal(PayoutAccountStatus.Enabled, await GetStatusAsync(aliceId));
    }

    [Fact]
    public async Task AccountUpdated_NewerEventAfterOlderOne_UpdatesStatus()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var now = DateTimeOffset.UtcNow;
        var enabledEvent = StripeWebhookPayloads.AccountUpdated(
            AccountId,
            transfers: "active",
            detailsSubmitted: true,
            created: now.AddMinutes(-5));
        var restrictedEvent = StripeWebhookPayloads.AccountUpdated(
            AccountId,
            transfers: "inactive",
            detailsSubmitted: true,
            created: now,
            outstandingRequirements: ["individual.verification.document"],
            disabledReason: "requirements.past_due");

        // Act
        await client.SendAsync(enabledEvent.ToRequest());
        await client.SendAsync(restrictedEvent.ToRequest());

        // Assert
        Assert.Equal(PayoutAccountStatus.Restricted, await GetStatusAsync(aliceId));
    }

    [Fact]
    public async Task AccountUpdated_SameSecondEventsDeliveredOutOfOrder_UsesTheLiveAccountState()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var created = DateTimeOffset.UtcNow;
        var newerEvent = StripeWebhookPayloads.AccountUpdated(AccountId, transfers: "active", detailsSubmitted: true, created: created);
        var olderEvent = StripeWebhookPayloads.AccountUpdated(AccountId, transfers: "pending", detailsSubmitted: true, created: created);
        Factory.StripeGateway.AccountResult = accountId => ActiveAccount(accountId);

        // Act
        await client.SendAsync(newerEvent.ToRequest());
        var olderResponse = await client.SendAsync(olderEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, olderResponse.StatusCode);
        Assert.Equal(PayoutAccountStatus.Enabled, await GetStatusAsync(aliceId));
        Assert.Equal([AccountId], Factory.StripeGateway.CallsTo<string>(nameof(IStripeGateway.GetAccountAsync)));
    }

    [Fact]
    public async Task AccountUpdated_SameSecondEvent_Returns500_AndKeepsStatus_WhenStripeCannotBeReached()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var created = DateTimeOffset.UtcNow;
        var firstEvent = StripeWebhookPayloads.AccountUpdated(AccountId, transfers: "active", detailsSubmitted: true, created: created);
        var secondEvent = StripeWebhookPayloads.AccountUpdated(AccountId, transfers: "pending", detailsSubmitted: true, created: created);
        Factory.StripeGateway.AccountResult = _ => throw new HttpRequestException("Connection reset.");

        // Act
        await client.SendAsync(firstEvent.ToRequest());
        var secondResponse = await client.SendAsync(secondEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, secondResponse.StatusCode);
        Assert.Equal(PayoutAccountStatus.Enabled, await GetStatusAsync(aliceId));
        Assert.False(await DbContext.ProcessedStripeEvents.AsNoTracking().AnyAsync(e => e.Id == secondEvent.EventId));
    }

    [Fact]
    public async Task AccountUpdated_DoesNotOverwriteANewerStatus_WrittenWhileTheEventIsBeingHandled()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var now = DateTimeOffset.UtcNow;
        var olderEvent = StripeWebhookPayloads.AccountUpdated(
            AccountId,
            transfers: "inactive",
            detailsSubmitted: true,
            created: now.AddMinutes(-5),
            outstandingRequirements: ["individual.verification.document"],
            disabledReason: "requirements.past_due");
        Factory.CommandHook.RunOnce(
            command => command.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("[AspNetUsers]", StringComparison.Ordinal),
            () => DbContext.Users
                .Where(u => u.Id == aliceId)
                .ExecuteUpdate(s => s
                    .SetProperty(u => u.PayoutAccountStatus, PayoutAccountStatus.Enabled)
                    .SetProperty(u => u.PayoutAccountStatusChangedAt, now.UtcDateTime)));

        // Act
        var response = await client.SendAsync(olderEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PayoutAccountStatus.Enabled, await GetStatusAsync(aliceId));
    }

    [Fact]
    public async Task AccountUpdated_ForUnknownAccount_Returns200_AndChangesNoUser()
    {
        // Arrange
        await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.AccountUpdated("acct_test_unknown", transfers: "active", detailsSubmitted: true);

        // Act
        var response = await client.SendAsync(signedEvent.ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await DbContext.Users.AsNoTracking().AnyAsync(u => u.PayoutAccountStatus == PayoutAccountStatus.Enabled));
    }

    [Fact]
    public async Task AccountUpdated_ArrivingBeforeAccountIdIsSaved_IsMatchedThroughUserIdMetadata()
    {
        // Arrange
        var bob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == "bob@example.com");
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.AccountUpdated(
            "acct_test_bob",
            transfers: "active",
            detailsSubmitted: true,
            metadataUserId: bob.Id);

        // Act
        await client.SendAsync(signedEvent.ToRequest());

        // Assert
        var updatedBob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == bob.Id);
        Assert.Equal("acct_test_bob", updatedBob.StripeAccountId);
        Assert.Equal(PayoutAccountStatus.Enabled, updatedBob.PayoutAccountStatus);
    }

    [Fact]
    public async Task AccountUpdated_WithUserIdMetadata_DoesNotReplaceAnotherAccountTheUserAlreadyHas()
    {
        // Arrange
        var aliceId = await GiveAliceConnectedAccountAsync();
        using var client = Factory.CreateClient();
        var signedEvent = StripeWebhookPayloads.AccountUpdated(
            "acct_test_other",
            transfers: "active",
            detailsSubmitted: true,
            metadataUserId: aliceId);

        // Act
        await client.SendAsync(signedEvent.ToRequest());

        // Assert
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == aliceId);
        Assert.Equal(AccountId, alice.StripeAccountId);
        Assert.Equal(PayoutAccountStatus.Pending, alice.PayoutAccountStatus);
    }

    private async Task<int> GiveAliceConnectedAccountAsync()
    {
        var alice = await DbContext.Users.FirstAsync(u => u.Email == "alice@example.com");
        alice.StripeAccountId = AccountId;
        alice.PayoutAccountStatus = PayoutAccountStatus.Pending;
        await DbContext.SaveChangesAsync();
        return alice.Id;
    }

    private async Task<PayoutAccountStatus> GetStatusAsync(int userId)
        => (await DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).PayoutAccountStatus;

    private static Account ActiveAccount(string accountId) => new()
    {
        Id = accountId,
        Object = "account",
        DetailsSubmitted = true,
        Capabilities = new AccountCapabilities { Transfers = "active" }
    };
}
