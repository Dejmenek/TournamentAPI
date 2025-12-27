using TournamentAPI.IntegrationTests.GraphQL.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Matches;
public class MatchQueryTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public MatchQueryTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetMatchesForRound_WithBasicFields_ReturnsMatches()
    {
        // Act
        var response = await _fixture.Client.ExecuteQueryAsync<MatchesForRoundResponse>(
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
        var response = await _fixture.Client.ExecuteQueryAsync<MatchesForRoundResponse>(
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
