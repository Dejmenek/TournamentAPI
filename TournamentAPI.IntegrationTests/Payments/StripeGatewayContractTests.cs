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
