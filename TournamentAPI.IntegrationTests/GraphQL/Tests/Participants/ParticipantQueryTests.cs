using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Participants;

public class ParticipantQueryTests : BaseIntegrationTest
{
    public ParticipantQueryTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTournamentParticipants_WithDescendingSlotNumberSort_ReturnsInDescendingOrder()
    {
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsSortedBySlotNumberDescending,
            new { id = 3 });

        Assert.False(response.HasErrors);
        var participantIds = response.Data!.TournamentById!.Participants!.Nodes!.Select(n => n.ParticipantId).ToList();
        Assert.Equal([8, 7, 6, 5, 4, 3, 2, 1], participantIds);
    }

    [Fact]
    public async Task GetTournamentParticipants_WithAscendingSlotNumberSort_ReturnsInAscendingOrder()
    {
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsSortedBySlotNumberAscending,
            new { id = 3 });

        Assert.False(response.HasErrors);
        var participantIds = response.Data!.TournamentById!.Participants!.Nodes!.Select(n => n.ParticipantId).ToList();
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], participantIds);
    }

    [Fact]
    public async Task GetTournamentParticipants_WithSlotNumberFilter_ReturnsOnlyMatchingParticipants()
    {
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsFilteredBySlotNumberGreaterThan,
            new { id = 3, minSlot = 5 });

        Assert.False(response.HasErrors);
        var participants = response.Data!.TournamentById!.Participants!;
        Assert.Equal(3, participants.TotalCount);
        Assert.Equal([6, 7, 8], participants.Nodes!.Select(n => n.ParticipantId).OrderBy(id => id));
    }

    [Fact]
    public async Task GetTournamentParticipants_WithParticipantIdFilter_ReturnsOnlyThatParticipant()
    {
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipantsFilteredByParticipantId,
            new { id = 3, participantId = 3 });

        Assert.False(response.HasErrors);
        var participants = response.Data!.TournamentById!.Participants!;
        Assert.Equal(1, participants.TotalCount);
        Assert.Equal(3, participants.Nodes!.Single().ParticipantId);
    }
}
