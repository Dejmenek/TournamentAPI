using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Matches;
public class MatchQueryTests : BaseIntegrationTest
{
    public MatchQueryTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMatchesForRound_WithBasicFields_ReturnsMatches()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<MatchesForRoundResponse>(
            QueryExamples.Queries.Match.GetMatchesForRoundWithBasicFields,
            new
            {
                tournamentId = 3,
                roundNumber = 1
            });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.MatchesForRound);
    }

    [Fact]
    public async Task GetMatchesForRound_WithPlayerDetails_ReturnsMatchesWithPlayers()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<MatchesForRoundResponse>(
            QueryExamples.Queries.Match.GetMatchesForRoundWithPlayerDetails,
            new
            {
                tournamentId = 3,
                roundNumber = 1
            });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.MatchesForRound);

        foreach (var match in response.Data.MatchesForRound!)
        {
            Assert.NotNull(match.Player1);
            if (match.Player2Id.HasValue)
                Assert.NotNull(match.Player2);
            if (match.WinnerId.HasValue)
                Assert.NotNull(match.Winner);
        }
    }
}
