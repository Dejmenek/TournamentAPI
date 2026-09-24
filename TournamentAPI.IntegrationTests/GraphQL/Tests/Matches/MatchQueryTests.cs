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

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Match.GetMatchesForRoundWithBasicFields,
            new
            {
                tournamentId = 3,
                roundNumber = 1
            });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.TournamentById);
        Assert.NotNull(response.Data.TournamentById.Bracket);
        Assert.NotNull(response.Data.TournamentById.Bracket.MatchesByBracket);
    }

    [Fact]
    public async Task GetMatchesForRound_WithPlayerDetails_ReturnsMatchesWithPlayers()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Match.GetMatchesForRoundWithPlayerDetails,
            new
            {
                tournamentId = 3,
                roundNumber = 1
            });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.TournamentById);
        Assert.NotNull(response.Data.TournamentById.Bracket);
        Assert.NotNull(response.Data.TournamentById.Bracket.MatchesByBracket);

        foreach (var match in response.Data.TournamentById.Bracket.MatchesByBracket.Nodes!)
        {
            Assert.NotNull(match.Player1);
            if (match.Player2Id.HasValue)
                Assert.NotNull(match.Player2);
            if (match.WinnerId.HasValue)
                Assert.NotNull(match.Winner);
        }
    }

    [Fact]
    public async Task GetMatchesForRound_WithRoundFilter_ExcludesOtherRounds()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Match.GetMatchesForRoundWithBasicFields,
            new
            {
                tournamentId = 3,
                roundNumber = 1
            });

        // Assert
        Assert.False(response.HasErrors);
        var matches = response.Data!.TournamentById!.Bracket!.MatchesByBracket!;
        Assert.Equal(4, matches.TotalCount);
        Assert.All(matches.Nodes!, m => Assert.Equal(1, m.Round));
    }

    [Fact]
    public async Task GetMatchesForRound_WithNonExistentRound_ReturnsEmptyConnection()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Match.GetMatchesForRoundWithBasicFields,
            new
            {
                tournamentId = 3,
                roundNumber = 99
            });

        // Assert
        Assert.False(response.HasErrors);
        var matches = response.Data!.TournamentById!.Bracket!.MatchesByBracket!;
        Assert.Equal(0, matches.TotalCount);
        Assert.Empty(matches.Nodes!);
    }
}
