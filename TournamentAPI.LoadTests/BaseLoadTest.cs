using TournamentAPI.Shared.Helpers;

namespace TournamentAPI.LoadTests;

[Collection(nameof(LoadTestCollection))]
public abstract class BaseLoadTest
{
    protected readonly LoadTestWebAppFactory Factory;
    protected BaseLoadTest(LoadTestWebAppFactory factory)
    {
        Factory = factory;
    }

    protected TestClient CreateClient()
    {
        var httpClient = Factory.CreateClient();
        return new TestClient(httpClient);
    }
}
