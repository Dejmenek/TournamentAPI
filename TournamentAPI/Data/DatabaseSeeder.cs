using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        bool recreateDatabase = false)
    {
        if (recreateDatabase)
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        var user1 = new ApplicationUser { UserName = "alice", Email = "alice@example.com", FirstName = "Alice", LastName = "Smith" };
        var user2 = new ApplicationUser { UserName = "bob", Email = "bob@example.com", FirstName = "Bob", LastName = "Johnson" };
        var user3 = new ApplicationUser { UserName = "carol", Email = "carol@example.com", FirstName = "Carol", LastName = "Williams" };

        user1 = await EnsureUserAsync(userManager, user1, "Password123!");
        user2 = await EnsureUserAsync(userManager, user2, "Password123!");
        user3 = await EnsureUserAsync(userManager, user3, "Password123!");

        user1 = await context.Users.FirstAsync(u => u.UserName == user1.UserName);
        user2 = await context.Users.FirstAsync(u => u.UserName == user2.UserName);
        user3 = await context.Users.FirstAsync(u => u.UserName == user3.UserName);

        if (!context.Tournaments.Any())
        {
            var tournament1 = new Tournament
            {
                Name = "Spring Invitational",
                StartDate = DateTime.UtcNow.AddDays(7),
                Status = TournamentStatus.Open,
                OwnerId = user1.Id,
                Owner = user1,
                Participants = new List<TournamentParticipant>()
            };
            var tournament2 = new Tournament
            {
                Name = "Summer Cup",
                StartDate = DateTime.UtcNow.AddDays(30),
                Status = TournamentStatus.Open,
                OwnerId = user2.Id,
                Owner = user2,
                Participants = new List<TournamentParticipant>()
            };

            tournament1.Participants.Add(new TournamentParticipant { Tournament = tournament1, Participant = user1 });
            tournament1.Participants.Add(new TournamentParticipant { Tournament = tournament1, Participant = user2 });

            tournament2.Participants.Add(new TournamentParticipant { Tournament = tournament2, Participant = user2 });
            tournament2.Participants.Add(new TournamentParticipant { Tournament = tournament2, Participant = user3 });

            context.Tournaments.AddRange(tournament1, tournament2);
            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password)
    {
        var existing = await userManager.FindByNameAsync(user.UserName!);
        if (existing == null)
        {
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user {user.UserName}: {errors}");
            }
            return user;
        }
        return existing;
    }
}
