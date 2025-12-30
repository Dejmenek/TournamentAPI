using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Benchmarks;

internal static class BenchmarkDatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        DataSize dataSize)
    {
        var (userCount, tournamentCount, participantsPerTournament) = dataSize switch
        {
            DataSize.Small => (10, 5, 4),
            DataSize.Medium => (50, 25, 8),
            DataSize.Large => (200, 100, 16),
            _ => throw new ArgumentOutOfRangeException(nameof(dataSize))
        };

        // Create users
        var users = new List<ApplicationUser>();
        for (int i = 0; i < userCount; i++)
        {
            var user = new ApplicationUser
            {
                UserName = $"benchmark_user{i}",
                Email = $"benchmark_user{i}@benchmark.com",
                FirstName = $"BenchFirstName{i}",
                LastName = $"BenchLastName{i}"
            };

            var result = await userManager.CreateAsync(user, "BenchmarkPassword123!");
            if (result.Succeeded)
            {
                users.Add(await context.Users.FirstAsync(u => u.UserName == user.UserName));
            }
        }

        // Create tournaments
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < tournamentCount; i++)
        {
            var owner = users[random.Next(users.Count)];
            var status = random.Next(2) == 0 ? TournamentStatus.Open : TournamentStatus.Closed;

            var tournament = new Tournament
            {
                Name = $"Benchmark Tournament {i}",
                StartDate = DateTime.UtcNow.AddDays(random.Next(-30, 30)),
                Status = status,
                OwnerId = owner.Id,
                Owner = owner,
                Participants = new List<TournamentParticipant>()
            };

            // Add participants
            var selectedUsers = users
                .OrderBy(_ => random.Next())
                .Take(Math.Min(participantsPerTournament, users.Count))
                .ToList();

            foreach (var user in selectedUsers)
            {
                tournament.Participants.Add(new TournamentParticipant
                {
                    Tournament = tournament,
                    Participant = user
                });
            }

            // Create bracket for closed tournaments
            if (status == TournamentStatus.Closed && selectedUsers.Count >= 2)
            {
                tournament.Bracket = new Bracket
                {
                    Matches = new List<Match>()
                };

                // Create first round matches
                for (int j = 0; j < selectedUsers.Count / 2; j++)
                {
                    var match = new Match
                    {
                        Round = 1,
                        Player1Id = selectedUsers[j * 2].Id,
                        Player2Id = j * 2 + 1 < selectedUsers.Count ? selectedUsers[j * 2 + 1].Id : null,
                        WinnerId = random.Next(2) == 0 ? selectedUsers[j * 2].Id : (j * 2 + 1 < selectedUsers.Count ? selectedUsers[j * 2 + 1].Id : selectedUsers[j * 2].Id),
                        Bracket = tournament.Bracket
                    };
                    tournament.Bracket.Matches.Add(match);
                }
            }

            context.Tournaments.Add(tournament);
        }

        await context.SaveChangesAsync();
    }
}
