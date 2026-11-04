using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Stripe;
using TournamentAPI.Data.Models;
using TournamentAPI.Payments;
using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Payments;

public class PaymentMutationTests : BaseIntegrationTest
{
    private const string AliceEmail = "alice@example.com";

    public PaymentMutationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task StartStripeOnboarding_CreatesConnectedAccount_AndReturnsOnboardingLink_OnFirstCall()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.False(response.HasErrors);
        var link = response.Data?.StartStripeOnboarding?.StripeOnboardingLink;
        Assert.NotNull(link);
        Assert.Equal($"https://connect.stripe.com/setup/e/acct_test_user_{alice.Id}/link_1", link.Url);

        var accountRequest = Assert.Single(Factory.StripeGateway.CallsTo<ConnectedAccountRequest>(nameof(IStripeGateway.CreateConnectedAccountAsync)));
        Assert.Equal(new ConnectedAccountRequest(alice.Id, "DE", AliceEmail, Attempt: 0), accountRequest);

        var updatedAlice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == alice.Id);
        Assert.Equal($"acct_test_user_{alice.Id}", updatedAlice.StripeAccountId);
        Assert.Equal(PayoutAccountStatus.Pending, updatedAlice.PayoutAccountStatus);
    }

    [Fact]
    public async Task StartStripeOnboarding_ReusesExistingAccount_AndReturnsFreshLink_OnLaterCalls()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        var firstResponse = await StartOnboardingAsync(client, "DE");

        // Act
        var secondResponse = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.False(secondResponse.HasErrors);
        var firstUrl = firstResponse.Data?.StartStripeOnboarding?.StripeOnboardingLink?.Url;
        var secondUrl = secondResponse.Data?.StartStripeOnboarding?.StripeOnboardingLink?.Url;
        Assert.NotNull(secondUrl);
        Assert.NotEqual(firstUrl, secondUrl);

        Assert.Single(Factory.StripeGateway.CallsTo<ConnectedAccountRequest>(nameof(IStripeGateway.CreateConnectedAccountAsync)));

        var linkRequests = Factory.StripeGateway.CallsTo<OnboardingLinkRequest>(nameof(IStripeGateway.CreateOnboardingLinkAsync));
        Assert.Equal(2, linkRequests.Count);
        Assert.All(linkRequests, request => Assert.Equal($"acct_test_user_{alice.Id}", request.AccountId));
    }

    [Fact]
    public async Task StartStripeOnboarding_KeepsNewerWebhookStatus_WhenAccountUpdatedArrivesBeforeTheAccountIdIsSaved()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        var accountId = $"acct_test_user_{alice.Id}";
        Factory.StripeGateway.ConnectedAccountResult = request =>
        {
            DbContext.Users
                .Where(u => u.Id == request.UserId)
                .ExecuteUpdate(s => s
                    .SetProperty(u => u.StripeAccountId, accountId)
                    .SetProperty(u => u.PayoutAccountStatus, PayoutAccountStatus.Enabled)
                    .SetProperty(u => u.PayoutAccountStatusChangedAt, DateTime.UtcNow));
            return new Account { Id = accountId, Object = "account" };
        };

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.False(response.HasErrors);
        var updatedAlice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == alice.Id);
        Assert.Equal(accountId, updatedAlice.StripeAccountId);
        Assert.Equal(PayoutAccountStatus.Enabled, updatedAlice.PayoutAccountStatus);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("GB")]
    [InlineData("CH")]
    public async Task StartStripeOnboarding_ReturnsUnsupportedCountryError_AndCreatesNoAccount_WhenCountryIsOutsideEuEea(string country)
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await StartOnboardingAsync(client, country);

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Equal(PaymentErrorCodes.UnsupportedPayoutCountry, response.Errors![0].Extensions!["code"]?.ToString());

        Assert.Empty(Factory.StripeGateway.Calls);

        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        Assert.Null(alice.StripeAccountId);
        Assert.Equal(PayoutAccountStatus.NotStarted, alice.PayoutAccountStatus);
    }

    [Fact]
    public async Task StartStripeOnboarding_AcceptsLowercaseCountryCode()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await StartOnboardingAsync(client, "pl");

        // Assert
        Assert.False(response.HasErrors);
        var accountRequest = Assert.Single(Factory.StripeGateway.CallsTo<ConnectedAccountRequest>(nameof(IStripeGateway.CreateConnectedAccountAsync)));
        Assert.Equal("PL", accountRequest.Country);
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsStripeRequestFailedError_AndStoresNoAccount_WhenStripeRejectsTheRequest()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        Factory.StripeGateway.ConnectedAccountResult = _ => throw new StripeException("Stripe is unavailable.");

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Equal(PaymentErrorCodes.StripeRequestFailed, response.Errors![0].Extensions!["code"]?.ToString());

        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        Assert.Null(alice.StripeAccountId);
    }

    [Fact]
    public async Task StartStripeOnboarding_BuildsLinkForStoredAccount_WhenAnotherAccountWasLinkedWhileCreating()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        const string storedAccountId = "acct_linked_by_webhook";
        Factory.StripeGateway.ConnectedAccountResult = request =>
        {
            DbContext.Users
                .Where(u => u.Id == request.UserId)
                .ExecuteUpdate(s => s
                    .SetProperty(u => u.StripeAccountId, storedAccountId)
                    .SetProperty(u => u.PayoutAccountStatus, PayoutAccountStatus.Pending));
            return new Account { Id = $"acct_test_user_{request.UserId}", Object = "account" };
        };

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.False(response.HasErrors);
        var linkRequest = Assert.Single(Factory.StripeGateway.CallsTo<OnboardingLinkRequest>(nameof(IStripeGateway.CreateOnboardingLinkAsync)));
        Assert.Equal(storedAccountId, linkRequest.AccountId);

        var updatedAlice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == alice.Id);
        Assert.Equal(storedAccountId, updatedAlice.StripeAccountId);
    }

    [Fact]
    public async Task StartStripeOnboarding_UsesNextAttempt_OnRetry_AfterStripeReturnedAnError()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        Factory.StripeGateway.ConnectedAccountResult = _ =>
            throw new StripeException(HttpStatusCode.InternalServerError, new StripeError(), "Stripe is unavailable.");
        await StartOnboardingAsync(client, "DE");
        Factory.StripeGateway.ConnectedAccountResult = request => new Account { Id = $"acct_test_user_{request.UserId}", Object = "account" };

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.False(response.HasErrors);
        var attempts = Factory.StripeGateway.CallsTo<ConnectedAccountRequest>(nameof(IStripeGateway.CreateConnectedAccountAsync))
            .Select(r => r.Attempt);
        Assert.Equal([0, 1], attempts);
    }

    [Fact]
    public async Task StartStripeOnboarding_KeepsAttempt_OnRetry_AfterStripeReportedAConflict()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        Factory.StripeGateway.ConnectedAccountResult = _ =>
            throw new StripeException(HttpStatusCode.Conflict, new StripeError(), "A request with this idempotency key is in progress.");
        await StartOnboardingAsync(client, "DE");
        Factory.StripeGateway.ConnectedAccountResult = request => new Account { Id = $"acct_test_user_{request.UserId}", Object = "account" };

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.False(response.HasErrors);
        var attempts = Factory.StripeGateway.CallsTo<ConnectedAccountRequest>(nameof(IStripeGateway.CreateConnectedAccountAsync))
            .Select(r => r.Attempt);
        Assert.Equal([0, 0], attempts);
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsStripeRequestFailedError_AndKeepsAttempt_WhenTheConnectionFails()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        Factory.StripeGateway.ConnectedAccountResult = _ => throw new HttpRequestException("Connection reset.");

        // Act
        var response = await StartOnboardingAsync(client, "DE");
        Factory.StripeGateway.ConnectedAccountResult = request => new Account { Id = $"acct_test_user_{request.UserId}", Object = "account" };
        await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Equal(PaymentErrorCodes.StripeRequestFailed, response.Errors![0].Extensions!["code"]?.ToString());

        var attempts = Factory.StripeGateway.CallsTo<ConnectedAccountRequest>(nameof(IStripeGateway.CreateConnectedAccountAsync))
            .Select(r => r.Attempt);
        Assert.Equal([0, 0], attempts);
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsStripeRequestFailedError_AndStoresNoAccount_WhenTheStripeRequestTimesOut()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        Factory.StripeGateway.ConnectedAccountResult = _ => throw new TaskCanceledException("The request timed out.");

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Equal(PaymentErrorCodes.StripeRequestFailed, response.Errors![0].Extensions!["code"]?.ToString());

        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        Assert.Null(alice.StripeAccountId);
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsStripeRequestFailedError_AndKeepsTheSavedAccount_WhenTheOnboardingLinkRequestFails()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        Factory.StripeGateway.OnboardingLinkResult = _ => throw new HttpRequestException("Connection reset.");

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Equal(PaymentErrorCodes.StripeRequestFailed, response.Errors![0].Extensions!["code"]?.ToString());

        var updatedAlice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == alice.Id);
        Assert.Equal($"acct_test_user_{alice.Id}", updatedAlice.StripeAccountId);
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsPayoutAccountSaveFailedError_WhenSavingTheAccountIdFails()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        Factory.StripeGateway.ConnectedAccountResult = request =>
        {
            DbContext.Database.ExecuteSqlRaw(
                "CREATE TRIGGER FailUserUpdates ON AspNetUsers AFTER UPDATE AS THROW 50000, 'Simulated save failure.', 1;");
            return new Account { Id = $"acct_test_user_{request.UserId}", Object = "account" };
        };

        GraphQLResponse<StartStripeOnboardingResponse> response;
        try
        {
            // Act
            response = await StartOnboardingAsync(client, "DE");
        }
        finally
        {
            await DbContext.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS FailUserUpdates;");
        }

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Equal(PaymentErrorCodes.PayoutAccountSaveFailed, response.Errors![0].Extensions!["code"]?.ToString());
        Assert.Empty(Factory.StripeGateway.CallsTo<OnboardingLinkRequest>(nameof(IStripeGateway.CreateOnboardingLinkAsync)));

        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        Assert.Null(alice.StripeAccountId);
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsFreshLinkForExistingAccount_WhenCountryIsNoLongerSupported()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == AliceEmail);
        await StartOnboardingAsync(client, "DE");

        // Act
        var response = await StartOnboardingAsync(client, "US");

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Single(Factory.StripeGateway.CallsTo<ConnectedAccountRequest>(nameof(IStripeGateway.CreateConnectedAccountAsync)));

        var linkRequests = Factory.StripeGateway.CallsTo<OnboardingLinkRequest>(nameof(IStripeGateway.CreateOnboardingLinkAsync));
        Assert.All(linkRequests, request => Assert.Equal($"acct_test_user_{alice.Id}", request.AccountId));
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsPayoutAccountSaveFailedError_WhenTheUserIsGoneBeforeTheAccountIdIsSaved()
    {
        // Arrange
        const string email = "nora@example.com";
        using var client = CreateClient();
        await client.ExecuteMutationAsync<RegisterResponse>(
            Shared.MutationExamples.Mutations.Users.RegisterUser,
            new { input = new { email, userName = "nora", password = "Password123!" } });
        var authenticated = await CreateClientLoggedInAsAsync(email);
        Factory.StripeGateway.ConnectedAccountResult = request =>
        {
            DbContext.RefreshTokens.Where(t => t.UserId == request.UserId).ExecuteDelete();
            DbContext.Users.Where(u => u.Id == request.UserId).ExecuteDelete();
            return new Account { Id = $"acct_test_user_{request.UserId}", Object = "account" };
        };

        // Act
        var response = await StartOnboardingAsync(authenticated, "DE");

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Equal(PaymentErrorCodes.PayoutAccountSaveFailed, response.Errors![0].Extensions!["code"]?.ToString());
        Assert.Empty(Factory.StripeGateway.CallsTo<OnboardingLinkRequest>(nameof(IStripeGateway.CreateOnboardingLinkAsync)));
    }

    [Fact]
    public async Task StartStripeOnboarding_ReturnsError_WhenNotAuthenticated()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await StartOnboardingAsync(client, "DE");

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.StartStripeOnboarding?.StripeOnboardingLink);
        Assert.Empty(Factory.StripeGateway.Calls);
    }

    private async Task<TestClient> CreateClientLoggedInAsAsync(string email)
    {
        var client = CreateClient();
        var token = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email, password = "Password123!" } });
        client.SetAuthToken(token.Data!.LoginUser!.String!);
        return client;
    }

    private static Task<GraphQLResponse<StartStripeOnboardingResponse>> StartOnboardingAsync(TestClient client, string country) =>
        client.ExecuteMutationAsync<StartStripeOnboardingResponse>(
            Shared.MutationExamples.Mutations.Payments.StartStripeOnboarding,
            new { input = new { country } });
}
