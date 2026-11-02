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
}
