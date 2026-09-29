using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.LoadTests;

public enum LoadTestDataSize
{
    Default,
    Medium,
    Large
}

internal static class LoadTestDataSeeder
{
    public const string UserPassword = "LoadTestPassword123!";
    public const string TournamentNamePrefix = "Load Tournament";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        LoadTestDataSize size)
    {
        var (userCount, tournamentCount, participantsPerTournament) = size switch
        {
            LoadTestDataSize.Medium => (50, 25, 8),
            LoadTestDataSize.Large => (200, 100, 16),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Only Medium and Large are seeded here.")
        };

        for (var i = 0; i < userCount; i++)
        {
            var user = new ApplicationUser
            {
                UserName = $"loaduser{i}",
                Email = $"loaduser{i}@load.test",
                FirstName = $"LoadFirstName{i}",
                LastName = $"LoadLastName{i}",
                IsEmailPublic = true
            };

            var result = await userManager.CreateAsync(user, UserPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user {user.UserName}: {errors}");
            }
        }

        var users = await context.Users.OrderBy(u => u.Id).ToListAsync();
        var random = new Random(42);

        for (var i = 0; i < tournamentCount; i++)
        {
            var participants = users
                .OrderBy(_ => random.Next())
                .Take(Math.Min(participantsPerTournament, users.Count))
                .ToList();

            context.Tournaments.Add(BuildTournament(i, participants, participantsPerTournament, random));
        }

        await context.SaveChangesAsync();
    }

    private static Tournament BuildTournament(
        int index,
        IReadOnlyList<ApplicationUser> participants,
        int maxParticipants,
        Random random)
    {
        var kind = index % 4;
        var owner = participants[0];

        var tournament = new Tournament
        {
            Name = $"{TournamentNamePrefix} {index}",
            StartDate = DateTime.UtcNow.AddDays(random.Next(-30, 30)),
            OwnerId = owner.Id,
            Owner = owner,
            MaxParticipants = maxParticipants,
            Status = kind == 0 ? TournamentStatus.Open : kind == 2 ? TournamentStatus.Completed : TournamentStatus.Closed,
            Format = kind == 3 ? TournamentFormat.RoundRobin : TournamentFormat.SingleElimination,
            Participants = new List<TournamentParticipant>()
        };

        for (var slot = 0; slot < participants.Count; slot++)
        {
            tournament.Participants.Add(new TournamentParticipant
            {
                Tournament = tournament,
                Participant = participants[slot],
                SlotNumber = slot + 1
            });
        }

        if (kind == 0)
        {
            return tournament;
        }

        tournament.Bracket = new Bracket { Matches = new List<Match>() };

        if (kind == 3)
        {
            AddRoundRobinMatches(tournament.Bracket, participants, random);
        }
        else
        {
            AddFirstRoundMatches(tournament.Bracket, participants, random, playAll: kind == 2);
        }

        if (kind == 2)
        {
            var champion = participants[0];
            tournament.ChampionId = champion.Id;
            tournament.Champion = champion;
        }

        return tournament;
    }

    private static void AddFirstRoundMatches(
        Bracket bracket,
        IReadOnlyList<ApplicationUser> participants,
        Random random,
        bool playAll)
    {
        for (var i = 0; i + 1 < participants.Count; i += 2)
        {
            var played = playAll || i % 4 == 0;
            bracket.Matches.Add(NewMatch(bracket, 1, participants[i], participants[i + 1], played, random));
        }
    }

    private static void AddRoundRobinMatches(
        Bracket bracket,
        IReadOnlyList<ApplicationUser> participants,
        Random random)
    {
        var pairing = 0;

        for (var i = 0; i < participants.Count; i++)
        {
            for (var j = i + 1; j < participants.Count; j++)
            {
                var played = pairing++ % 4 != 0;
                bracket.Matches.Add(NewMatch(bracket, 1, participants[i], participants[j], played, random));
            }
        }
    }

    private static Match NewMatch(
        Bracket bracket,
        int round,
        ApplicationUser player1,
        ApplicationUser player2,
        bool played,
        Random random)
    {
        return new Match
        {
            Round = round,
            Player1Id = player1.Id,
            Player2Id = player2.Id,
            WinnerId = played ? (random.Next(2) == 0 ? player1.Id : player2.Id) : null,
            Status = played ? MatchStatus.Played : MatchStatus.Scheduled,
            Bracket = bracket
        };
    }
}
