using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Tournaments;
public class TournamentQueryTests : BaseIntegrationTest
{
    public TournamentQueryTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTournaments_ReturnsAllTournamentsWithTotalCount()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithTotalCount);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments?.Edges);
        Assert.Equal(9, response.Data.Tournaments.TotalCount);
        Assert.Equal(9, response.Data.Tournaments.Edges.Count);

        var tournamentNames = response.Data.Tournaments.Nodes?.Select(t => t.Name).ToList();
        Assert.Contains("Spring Invitational", tournamentNames);
        Assert.Contains("Summer Cup", tournamentNames);
    }

    [Fact]
    public async Task GetTournaments_WithFilterByName_ReturnsMatchingTournaments()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithNameFilter,
            new { nameFilter = "Spring" });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments?.Edges);
        Assert.Single(response.Data.Tournaments.Edges);

        var tournament = response.Data.Tournaments.Nodes?.First();
        Assert.NotNull(tournament);
        Assert.Equal("Spring Invitational", tournament.Name);
    }

    [Fact]
    public async Task GetTournaments_WithExcessivePageSize_ReturnsMaxAllowedItemsError()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithExcessivePageSize);

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.Tournaments);

        var maxAllowedItems = ((System.Text.Json.JsonElement)response.Errors?.First().Extensions?["maxAllowedItems"]!).GetInt32();
        Assert.Equal(100, maxAllowedItems);
    }

    [Fact]
    public async Task GetTournaments_WithoutPaging_ReturnsError()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetWithoutPaging);

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.Tournaments);
        var errorMessage = response.Errors?.First().Message;
        Assert.Contains("Exactly one slicing argument must be defined.", errorMessage!);
    }

    [Fact]
    public async Task GetTournaments_WithParticipants_ReturnsTournamentsWithParticipants()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithParticipants);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments?.Edges);
        Assert.Equal(9, response.Data.Tournaments.TotalCount);
        Assert.Equal(9, response.Data.Tournaments.Edges.Count);

        var springTournament = response.Data.Tournaments.Nodes?.FirstOrDefault(t => t.Name == "Spring Invitational");
        Assert.NotNull(springTournament);
        Assert.NotNull(springTournament.Participants);
        Assert.Equal(2, springTournament.Participants.Count);

        var summerTournament = response.Data.Tournaments.Nodes?.FirstOrDefault(t => t.Name == "Summer Cup");
        Assert.NotNull(summerTournament);
        Assert.NotNull(summerTournament.Participants);
        Assert.Equal(2, summerTournament.Participants.Count);
    }

    [Fact]
    public async Task GetTournaments_WithBracketAndMatches_ReturnsTournamentsWithBracketsAndMatches()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments?.Edges);
        Assert.Equal(9, response.Data.Tournaments.TotalCount);
        Assert.Equal(9, response.Data.Tournaments.Edges.Count);

        var springTournament = response.Data.Tournaments.Nodes?.FirstOrDefault(t => t.Name == "Spring Invitational");
        Assert.NotNull(springTournament);
        Assert.Null(springTournament.Bracket);

        var summerTournament = response.Data.Tournaments.Nodes?.FirstOrDefault(t => t.Name == "Summer Cup");
        Assert.NotNull(summerTournament);
        Assert.Null(summerTournament.Bracket);

        var winterTournament = response.Data.Tournaments.Nodes?.FirstOrDefault(t => t.Name == "Winter Championship 2024");
        Assert.NotNull(winterTournament);
        Assert.NotNull(winterTournament.Bracket);
        Assert.NotNull(winterTournament.Bracket.Matches);
        Assert.Equal(7, winterTournament.Bracket.Matches.Count);
    }

    [Fact]
    public async Task GetTournaments_WithOwner_ReturnsTournamentsWithOwnerDetails()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithOwner);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments?.Edges);
        Assert.Equal(9, response.Data.Tournaments.TotalCount);
        Assert.Equal(9, response.Data.Tournaments.Edges.Count);

        foreach (var tournament in response.Data.Tournaments.Nodes!)
        {
            Assert.NotNull(tournament.Owner);
            Assert.NotEmpty(tournament.Owner.FirstName);
            Assert.NotEmpty(tournament.Owner.LastName);
            Assert.NotEmpty(tournament.Owner.Email);
        }
    }

    [Fact]
    public async Task GetTournaments_WithDescendingNameSorting_ReturnsTournamentsInDescendingOrder()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithSorting);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments?.Edges);
        Assert.Equal(9, response.Data.Tournaments.TotalCount);
        Assert.Equal(9, response.Data.Tournaments.Edges.Count);

        var tournamentNames = response.Data.Tournaments.Nodes?.Select(t => t.Name).ToList();
        var sortedNames = tournamentNames?.OrderByDescending(n => n).ToList();
        Assert.Equal(sortedNames, tournamentNames);
    }

    [Fact]
    public async Task GetTournaments_ExcludesSoftDeletedTournaments()
    {
        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithTotalCount);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments?.Edges);

        Assert.Equal(9, response.Data.Tournaments.TotalCount);
        Assert.Equal(9, response.Data.Tournaments.Edges.Count);

        var tournamentNames = response.Data.Tournaments.Nodes?.Select(t => t.Name).ToList();

        Assert.DoesNotContain("Cancelled Spring Event", tournamentNames);
        Assert.DoesNotContain("Cancelled Championship", tournamentNames);
        Assert.DoesNotContain("Partially Cancelled Event", tournamentNames);

        Assert.Contains("Spring Invitational", tournamentNames);
        Assert.Contains("Summer Cup", tournamentNames);
        Assert.Contains("Winter Championship 2024", tournamentNames);
        Assert.Contains("Autumn Battle", tournamentNames);
        Assert.Contains("Quick Fire Tournament", tournamentNames);
    }

    [Fact]
    public async Task GetTournamentById_ReturnsCorrectTournament()
    {
        // Arrange
        var tournamentId = 1;

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetById,
            new { id = tournamentId });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.TournamentById);
        Assert.Equal("Spring Invitational", response.Data.TournamentById.Name);
        Assert.Equal(tournamentId, response.Data.TournamentById.Id);
    }

    [Fact]
    public async Task GetTournamentById_ReturnsNullForNonExistentId()
    {
        // Arrange
        var tournamentId = 999;

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetById,
            new { id = tournamentId });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Null(response.Data?.TournamentById);
    }

    [Fact]
    public async Task GetTournamentById_ReturnsNullForSoftDeleted()
    {
        // Arrange
        var tournamentId = 6;

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetById,
            new { id = tournamentId });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Null(response.Data?.TournamentById);
    }

    [Fact]
    public async Task GetTournamentById_WithOwner_ReturnsTournamentWithOwnerDetails()
    {
        // Arrange
        var tournamentId = 1;

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithOwner,
            new { id = tournamentId });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.TournamentById);
        Assert.Equal("Spring Invitational", response.Data.TournamentById.Name);
        Assert.Equal(tournamentId, response.Data.TournamentById.Id);

        var owner = response.Data.TournamentById.Owner;
        Assert.NotNull(owner);
        Assert.NotEmpty(owner.FirstName);
        Assert.NotEmpty(owner.LastName);
        Assert.NotEmpty(owner.Email);
    }

    [Fact]
    public async Task GetTournamentById_WithBracketAndMatches_ReturnsTournamentWithBracketAndMatches()
    {
        // Arrange
        var tournamentId = 3;

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithBracketAndMatches,
            new { id = tournamentId });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.TournamentById);
        Assert.Equal("Winter Championship 2024", response.Data.TournamentById.Name);
        Assert.Equal(tournamentId, response.Data.TournamentById.Id);

        var bracket = response.Data.TournamentById.Bracket;
        Assert.NotNull(bracket);
        Assert.NotNull(bracket.Matches);
        Assert.Equal(7, bracket.Matches.Count);
    }

    [Fact]
    public async Task GetTournamentById_WithParticipants_ReturnsTournamentWithParticipants()
    {
        // Arrange
        var tournamentId = 1;

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipants,
            new { id = tournamentId });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.TournamentById);
        Assert.Equal("Spring Invitational", response.Data.TournamentById.Name);
        Assert.Equal(tournamentId, response.Data.TournamentById.Id);

        var participants = response.Data.TournamentById.Participants;
        Assert.NotNull(participants);
        Assert.Equal(2, participants.Count);
    }
}
