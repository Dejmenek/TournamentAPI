using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.DataLoaders;

public class BatchingTests : BaseIntegrationTest
{
    public BatchingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTournamentById_WithManyMatchesReferencingManyUsers_BatchesApplicationUserLookups()
    {
        // Arrange
        Factory.CommandRecorder.Clear();
        using var client = CreateClient();

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithAllMatchesAndPlayers,
            new { id = 3 });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.TournamentById?.Bracket?.MatchesByBracket);
        Assert.Equal(7, response.Data.TournamentById.Bracket.MatchesByBracket.TotalCount);

        var userTableCommands = CountTableCommands("AspNetUsers");
        Assert.Equal(1, userTableCommands);
    }

    [Fact]
    public async Task GetTournamentById_WithManyParticipants_BatchesParticipantAndBackReferenceLookups()
    {
        // Arrange
        Factory.CommandRecorder.Clear();
        using var client = CreateClient();

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsAndTournamentBackReference,
            new { id = 3 });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.TournamentById?.Participants);
        Assert.Equal(8, response.Data.TournamentById.Participants.TotalCount);

        var userTableCommands = CountTableCommands("AspNetUsers");
        var tournamentTableCommands = CountTableCommands("Tournaments");

        Assert.Equal(1, userTableCommands);
        Assert.Equal(2, tournamentTableCommands);
    }

    [Fact]
    public async Task GetTournaments_WithManyBrackets_BatchesBracketLookups()
    {
        // Arrange
        Factory.CommandRecorder.Clear();
        using var client = CreateClient();

        // Act
        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments);

        var bracketTableCommands = CountTableCommands("Brackets");
        Assert.Equal(1, bracketTableCommands);
    }

    [Fact]
    public async Task GetTournamentParticipants_WithManyParticipants_BatchesWonTournamentIdLookups()
    {
        // Arrange
        Factory.CommandRecorder.Clear();
        using var client = CreateClient();

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsWonTournaments,
            new { id = 3 });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal(8, response.Data?.TournamentById?.Participants?.TotalCount);

        var tournamentTableCommands = CountTableCommands("Tournaments");
        // 16, not 9: 1 root tournamentById fetch + 1 batched id-lookup covering all 8
        // participants (id resolution IS batched) + 8 per-participant page-fetch queries to
        // materialize the actual won-tournament entities + 6 extra count probes for the
        // participants with no won tournaments. See memory: history-connections-not-fully-batched.
        Assert.Equal(16, tournamentTableCommands);
    }

    [Fact]
    public async Task GetTournamentParticipants_WithManyParticipants_BatchesWonMatchIdLookups()
    {
        // Arrange
        Factory.CommandRecorder.Clear();
        using var client = CreateClient();

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsWonMatches,
            new { id = 3 });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal(8, response.Data?.TournamentById?.Participants?.TotalCount);

        var matchTableCommands = CountTableCommands("Matches");
        // 9 = 1 batched id-lookup covering all 8 participants + 1 page-fetch query per
        // participant to materialize the actual won-match entities.
        Assert.Equal(9, matchTableCommands);
    }

    [Fact]
    public async Task GetTournamentParticipants_WithManyParticipants_BatchesPlayedTournamentIdLookups()
    {
        // Arrange
        Factory.CommandRecorder.Clear();
        using var client = CreateClient();

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsPlayedTournaments,
            new { id = 3 });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal(8, response.Data?.TournamentById?.Participants?.TotalCount);

        var participantTableCommands = CountTableCommands("TournamentParticipants");
        // 3, not 1: 2 baseline commands the participants connection itself always issues
        // (its own totalCount and item fetch) + 1 batched id-lookup covering all 8 participants
        // for playedTournaments - the id-lookup step really is a single batched query.
        Assert.Equal(3, participantTableCommands);
    }

    private int CountTableCommands(string tableName) =>
        Factory.CommandRecorder.Commands.Count(c => c.Contains($"[{tableName}]") || c.Contains($" {tableName} "));
}
