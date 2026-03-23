using Microsoft.AspNetCore.Mvc.Testing;
using TournamentAPI.Shared.Helpers;

namespace TournamentAPI.LoadTests;

public abstract class BaseLoadTest
{
    private readonly WebApplicationFactory<Program> _factory;

    protected BaseLoadTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    protected TestClient CreateClient()
    {
        var httpClient = _factory.CreateClient();
        return new TestClient(httpClient);
    }
}
