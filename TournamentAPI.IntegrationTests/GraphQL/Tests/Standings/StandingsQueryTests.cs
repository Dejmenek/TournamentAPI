using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Standings;

public class StandingsQueryTests : BaseIntegrationTest
{
    public StandingsQueryTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Standings_ReflectMidTournamentAndFinalCompletionWithCorrectChampion()
    {
        // Arrange: 4 participants, owned by emma (id 5) who is not herself a participant.
        var email = "emma@example.com";
        var password = "Password123!";
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email, password } });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var createResponse = await client.ExecuteMutationAsync<CreateTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.CreateTournamentWithBasicFieldsReturn,
            new
            {
                input = new
                {
                    name = "Standings Fixture",
                    startDate = DateTime.UtcNow.AddDays(1),
                    status = "OPEN",
                    maxParticipants = 4,
                    format = "ROUND_ROBIN"
                }
            });
        var tournamentId = createResponse.Data!.CreateTournament!.Tournament!.Id;

        foreach (var participantId in new[] { 1, 2, 3, 4 })
        {
            await client.ExecuteMutationAsync<AddParticipantResponse>(
                Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
                new { input = new { tournamentId, userId = participantId } });
        }

        await client.ExecuteMutationAsync<UpdateTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            new { input = new { tournamentId, status = "CLOSED" } });

        await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            new { input = new { tournamentId } });

        var bracketId = await DbContext.Brackets.AsNoTracking().Where(b => b.TournamentId == tournamentId).Select(b => b.Id).SingleAsync();

        async Task<int> MatchIdAsync(int a, int b) =>
            (await DbContext.Matches.AsNoTracking().SingleAsync(m =>
                m.BracketId == bracketId &&
                ((m.Player1Id == a && m.Player2Id == b) || (m.Player1Id == b && m.Player2Id == a)))).Id;

        var match1v2 = await MatchIdAsync(1, 2);
        var match1v3 = await MatchIdAsync(1, 3);
        var match1v4 = await MatchIdAsync(1, 4);
        var match2v3 = await MatchIdAsync(2, 3);
        var match2v4 = await MatchIdAsync(2, 4);
        var match3v4 = await MatchIdAsync(3, 4);

        // Act 1: play only two matches, both won by participant 1.
        await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = match1v2, winnerId = 1, player1Score = 3, player2Score = 1 } });
        await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = match1v3, winnerId = 1, player1Score = 3, player2Score = 1 } });

        var midResponse = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithBracketStandings,
            new { id = tournamentId });

        // Assert 1: standings only reflect completed matches.
        Assert.False(midResponse.HasErrors);
        var midStandings = midResponse.Data!.TournamentById!.Bracket!.Standings!;

        var p1Mid = midStandings.Single(s => s.ParticipantId == 1);
        Assert.Equal(2, p1Mid.Wins);
        Assert.Equal(6, p1Mid.Points);
        Assert.Equal(2, p1Mid.MatchesPlayed);
        Assert.Equal(1, p1Mid.MatchesRemaining);

        var p4Mid = midStandings.Single(s => s.ParticipantId == 4);
        Assert.Equal(0, p4Mid.Points);
        Assert.Equal(0, p4Mid.MatchesPlayed);
        Assert.Equal(3, p4Mid.MatchesRemaining);

        Assert.Equal("CLOSED", midResponse.Data.TournamentById.Status);

        // Act 2: play the remaining matches to completion.
        await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = match1v4, winnerId = 1, player1Score = 3, player2Score = 1 } });
        await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = match2v3, winnerId = 2, player1Score = 2, player2Score = 1 } });
        await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = match2v4, winnerId = (int?)null, player1Score = 1, player2Score = 1 } });
        await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = match3v4, winnerId = 3, player1Score = 2, player2Score = 1 } });

        var finalResponse = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithBracketStandings,
            new { id = tournamentId });

        // Assert 2: tournament completed, champion is the rank-1 participant.
        Assert.False(finalResponse.HasErrors);
        Assert.Equal("COMPLETED", finalResponse.Data!.TournamentById!.Status);
        Assert.Equal(1, finalResponse.Data.TournamentById.ChampionId);

        var finalStandings = finalResponse.Data.TournamentById.Bracket!.Standings!;
        var champion = finalStandings.Single(s => s.Rank == 1);
        Assert.Equal(1, champion.ParticipantId);
        Assert.Equal(9, champion.Points);

        var tournamentInDb = await DbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournamentId);
        Assert.Equal(TournamentStatus.Completed, tournamentInDb.Status);
        Assert.Equal(1, tournamentInDb.ChampionId);
    }
}
