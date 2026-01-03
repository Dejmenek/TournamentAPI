namespace TournamentAPI.LoadTests;

[CollectionDefinition(nameof(LoadTestCollection))]
public class LoadTestCollection : ICollectionFixture<LoadTestWebAppFactory>
{
}