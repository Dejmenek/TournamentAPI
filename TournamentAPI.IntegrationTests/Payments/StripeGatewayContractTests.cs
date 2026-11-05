using TournamentAPI.IntegrationTests.Infrastructure;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests.Payments;

public class StripeGatewayContractTests : IClassFixture<StripeMockFixture>
{
    private readonly StripeGateway _gateway;

    public StripeGatewayContractTests(StripeMockFixture stripeMock)
    {
        _gateway = new StripeGateway(stripeMock.CreateClient());
    }

    [Fact]
    public async Task GetCheckoutSessionAsync_IsAcceptedByStripeApi()
    {
        // Act
        var session = await _gateway.GetCheckoutSessionAsync("cs_test_contract", CancellationToken.None);

        // Assert
        Assert.Equal("checkout.session", session.Object);
        Assert.False(string.IsNullOrEmpty(session.Id));
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_IsAcceptedByStripeApi()
    {
        // Act
        var session = await _gateway.CreateCheckoutSessionAsync(
            new CheckoutSessionRequest(
                IdempotencyKey: "prize-pool-tournament-1-0",
                Amount: 5250,
                Currency: "eur",
                ProductName: "Prize pool for Contract Cup",
                SuccessUrl: "https://example.com/prize-pool/funded",
                CancelUrl: "https://example.com/prize-pool/cancelled",
                TransferGroup: "tournament-1",
                Metadata: new Dictionary<string, string>
                {
                    [PrizePoolFunding.SessionTypeMetadataKey] = PrizePoolFunding.SessionTypeValue,
                    [PrizePoolFunding.TournamentIdMetadataKey] = "1",
                    [PrizePoolFunding.OwnerIdMetadataKey] = "42"
                },
                ExpiresAt: DateTime.UtcNow.AddMinutes(31)),
            CancellationToken.None);

        // Assert
        Assert.Equal("checkout.session", session.Object);
        Assert.False(string.IsNullOrEmpty(session.Id));
    }

    [Fact]
    public async Task GetPaymentIntentAsync_IsAcceptedByStripeApi()
    {
        // Act
        var paymentIntent = await _gateway.GetPaymentIntentAsync("pi_test_contract", CancellationToken.None);

        // Assert
        Assert.Equal("payment_intent", paymentIntent.Object);
        Assert.False(string.IsNullOrEmpty(paymentIntent.Id));
    }

    [Fact]
    public async Task GetAccountAsync_IsAcceptedByStripeApi()
    {
        // Act
        var account = await _gateway.GetAccountAsync("acct_test_contract", CancellationToken.None);

        // Assert
        Assert.Equal("account", account.Object);
        Assert.False(string.IsNullOrEmpty(account.Id));
    }

    [Fact]
    public async Task CreateConnectedAccountAsync_IsAcceptedByStripeApi()
    {
        // Act
        var account = await _gateway.CreateConnectedAccountAsync(
            new ConnectedAccountRequest(UserId: 42, Country: "DE", Email: "alice@example.com", Attempt: 0),
            CancellationToken.None);

        // Assert
        Assert.Equal("account", account.Object);
        Assert.False(string.IsNullOrEmpty(account.Id));
    }

    [Fact]
    public async Task CreateOnboardingLinkAsync_IsAcceptedByStripeApi()
    {
        // Act
        var link = await _gateway.CreateOnboardingLinkAsync(
            new OnboardingLinkRequest(
                AccountId: "acct_test_contract",
                RefreshUrl: "https://example.com/payouts/refresh",
                ReturnUrl: "https://example.com/payouts/return"),
            CancellationToken.None);

        // Assert
        Assert.Equal("account_link", link.Object);
        Assert.False(string.IsNullOrEmpty(link.Url));
    }
}
