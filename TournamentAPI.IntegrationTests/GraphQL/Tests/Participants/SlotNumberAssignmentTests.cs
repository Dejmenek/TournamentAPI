using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Participants;

public class SlotNumberAssignmentTests : BaseIntegrationTest
{
    public SlotNumberAssignmentTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    private async Task<TestClient> LoginAsync(string email, string password = "Password123!")
    {
        var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email, password } });
        client.SetAuthToken(tokenResponse.Data!.LoginUser!.String!);

        return client;
    }

    private async Task<Tournament> CreateOpenTournamentAsync(int ownerId, int maxParticipants)
    {
        var tournament = new Tournament
        {
            Name = "Slot Number Test Tournament",
            StartDate = DateTime.UtcNow.AddDays(7),
            Status = TournamentStatus.Open,
            OwnerId = ownerId,
            MaxParticipants = maxParticipants
        };

        DbContext.Tournaments.Add(tournament);
        await DbContext.SaveChangesAsync();

        return tournament;
    }

    [Fact]
    public async Task AddParticipant_SequentialAdds_AssignStrictlyIncreasingUniqueSlotNumbers()
    {
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "alice");
        var tournament = await CreateOpenTournamentAsync(alice.Id, maxParticipants: 5);
        using var owner = await LoginAsync("alice@example.com");

        var participantUserNames = new[] { "bob", "carol", "david", "emma" };
        var expectedSlot = 1;

        foreach (var userName in participantUserNames)
        {
            var user = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == userName);

            var response = await owner.ExecuteMutationAsync<AddParticipantResponse>(
                Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
                new { input = new { tournamentId = tournament.Id, userId = user.Id } });

            Assert.False(response.HasErrors);

            var slot = await DbContext.TournamentParticipants
                .AsNoTracking()
                .Where(tp => tp.TournamentId == tournament.Id && tp.ParticipantId == user.Id)
                .Select(tp => tp.SlotNumber)
                .SingleAsync();
            Assert.Equal(expectedSlot, slot);
            expectedSlot++;
        }

        var finalSlots = await DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournament.Id)
            .Select(tp => tp.SlotNumber)
            .OrderBy(s => s)
            .ToListAsync();
        Assert.Equal(Enumerable.Range(1, participantUserNames.Length), finalSlots);
    }

    [Fact]
    public async Task JoinTournament_SequentialJoins_AssignStrictlyIncreasingUniqueSlotNumbers()
    {
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "alice");
        var tournament = await CreateOpenTournamentAsync(alice.Id, maxParticipants: 5);

        var joinerEmails = new[] { "bob@example.com", "carol@example.com", "david@example.com", "emma@example.com" };
        var expectedSlot = 1;

        foreach (var email in joinerEmails)
        {
            using var client = await LoginAsync(email);

            var response = await client.ExecuteMutationAsync<JoinTournamentResponse>(
                Shared.MutationExamples.Mutations.Tournaments.JoinTournament,
                new { input = new { tournamentId = tournament.Id } });

            Assert.False(response.HasErrors);

            var user = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == email);
            var slot = await DbContext.TournamentParticipants
                .AsNoTracking()
                .Where(tp => tp.TournamentId == tournament.Id && tp.ParticipantId == user.Id)
                .Select(tp => tp.SlotNumber)
                .SingleAsync();
            Assert.Equal(expectedSlot, slot);
            expectedSlot++;
        }

        var finalSlots = await DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournament.Id)
            .Select(tp => tp.SlotNumber)
            .OrderBy(s => s)
            .ToListAsync();
        Assert.Equal(Enumerable.Range(1, joinerEmails.Length), finalSlots);
    }

    [Fact]
    public async Task AddParticipant_And_JoinTournament_Interleaved_ProduceNoGapsOrDuplicates()
    {
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "alice");
        var tournament = await CreateOpenTournamentAsync(alice.Id, maxParticipants: 5);
        using var owner = await LoginAsync("alice@example.com");

        var bob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "bob");
        var david = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "david");

        var addBobResponse = await owner.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            new { input = new { tournamentId = tournament.Id, userId = bob.Id } });
        Assert.False(addBobResponse.HasErrors);

        using (var carolClient = await LoginAsync("carol@example.com"))
        {
            var joinCarolResponse = await carolClient.ExecuteMutationAsync<JoinTournamentResponse>(
                Shared.MutationExamples.Mutations.Tournaments.JoinTournament,
                new { input = new { tournamentId = tournament.Id } });
            Assert.False(joinCarolResponse.HasErrors);
        }

        var addDavidResponse = await owner.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            new { input = new { tournamentId = tournament.Id, userId = david.Id } });
        Assert.False(addDavidResponse.HasErrors);

        using (var emmaClient = await LoginAsync("emma@example.com"))
        {
            var joinEmmaResponse = await emmaClient.ExecuteMutationAsync<JoinTournamentResponse>(
                Shared.MutationExamples.Mutations.Tournaments.JoinTournament,
                new { input = new { tournamentId = tournament.Id } });
            Assert.False(joinEmmaResponse.HasErrors);
        }

        var finalSlots = await DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournament.Id)
            .Select(tp => tp.SlotNumber)
            .OrderBy(s => s)
            .ToListAsync();
        Assert.Equal([1, 2, 3, 4], finalSlots);
    }

    [Fact]
    public async Task JoinTournament_AfterRaceCondition_WinningRequestHasCorrectSlotNumberAndNoGapExists()
    {
        var tournamentId = 14;
        using var client = await LoginAsync("carol@example.com");
        using var client2 = await LoginAsync("david@example.com");

        var variables = new { input = new { tournamentId } };

        var task1 = client.ExecuteMutationAsync<JoinTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.JoinTournament, variables);
        var task2 = client2.ExecuteMutationAsync<JoinTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.JoinTournament, variables);
        var results = await Task.WhenAll(task1, task2);

        Assert.Single(results, r => !r.HasErrors);
        Assert.Single(results, r => r.HasErrors);

        var tournament = await DbContext.Tournaments.AsNoTracking().FirstAsync(t => t.Id == tournamentId);
        var finalSlots = await DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.SlotNumber)
            .OrderBy(s => s)
            .ToListAsync();

        Assert.Equal(Enumerable.Range(1, tournament.MaxParticipants), finalSlots);
        Assert.Equal(tournament.MaxParticipants, finalSlots.Max());
    }

    [Fact]
    public async Task AddParticipant_AfterRaceCondition_WinningRequestHasCorrectSlotNumberAndNoGapExists()
    {
        var tournamentId = 14;
        var carol = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "carol");
        var david = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "david");
        using var client = await LoginAsync("emma@example.com");
        using var client2 = await LoginAsync("emma@example.com");

        var variables1 = new { input = new { tournamentId, userId = carol.Id } };
        var variables2 = new { input = new { tournamentId, userId = david.Id } };

        var task1 = client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn, variables1);
        var task2 = client2.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn, variables2);
        var results = await Task.WhenAll(task1, task2);

        Assert.Single(results, r => !r.HasErrors);
        Assert.Single(results, r => r.HasErrors);

        var tournament = await DbContext.Tournaments.AsNoTracking().FirstAsync(t => t.Id == tournamentId);
        var finalSlots = await DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.SlotNumber)
            .OrderBy(s => s)
            .ToListAsync();

        Assert.Equal(Enumerable.Range(1, tournament.MaxParticipants), finalSlots);
        Assert.Equal(tournament.MaxParticipants, finalSlots.Max());
    }
}
