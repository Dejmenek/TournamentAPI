using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Stripe;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed class StripeMockFixture : IAsyncLifetime
{
    private const int HttpPort = 12111;

    private readonly IContainer _container = new ContainerBuilder("stripe/stripe-mock:v0.206.0")
        .WithPortBinding(HttpPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
            .ForPort(HttpPort)
            .ForPath("/v1/charges")
            .ForStatusCodeMatching(_ => true)))
        .Build();

    public StripeClient CreateClient()
    {
        var apiBase = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpPort)}";
        return new StripeClient("sk_test_123", apiBase: apiBase);
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
