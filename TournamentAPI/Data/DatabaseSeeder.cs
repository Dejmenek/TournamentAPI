using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        // Create users
        var user1 = new ApplicationUser { UserName = "alice", Email = "alice@example.com", FirstName = "Alice", LastName = "Smith" };
        var user2 = new ApplicationUser { UserName = "bob", Email = "bob@example.com", FirstName = "Bob", LastName = "Johnson" };
        var user3 = new ApplicationUser { UserName = "carol", Email = "carol@example.com", FirstName = "Carol", LastName = "Williams" };
        var user4 = new ApplicationUser { UserName = "david", Email = "david@example.com", FirstName = "David", LastName = "Brown" };
        var user5 = new ApplicationUser { UserName = "emma", Email = "emma@example.com", FirstName = "Emma", LastName = "Davis" };
        var user6 = new ApplicationUser { UserName = "frank", Email = "frank@example.com", FirstName = "Frank", LastName = "Miller" };
        var user7 = new ApplicationUser { UserName = "grace", Email = "grace@example.com", FirstName = "Grace", LastName = "Wilson" };
        var user8 = new ApplicationUser { UserName = "henry", Email = "henry@example.com", FirstName = "Henry", LastName = "Moore" };

        user1 = await EnsureUserAsync(userManager, user1, "Password123!");
        user2 = await EnsureUserAsync(userManager, user2, "Password123!");
        user3 = await EnsureUserAsync(userManager, user3, "Password123!");
        user4 = await EnsureUserAsync(userManager, user4, "Password123!");
        user5 = await EnsureUserAsync(userManager, user5, "Password123!");
        user6 = await EnsureUserAsync(userManager, user6, "Password123!");
        user7 = await EnsureUserAsync(userManager, user7, "Password123!");
        user8 = await EnsureUserAsync(userManager, user8, "Password123!");

        user1 = await context.Users.FirstAsync(u => u.UserName == user1.UserName);
        user2 = await context.Users.FirstAsync(u => u.UserName == user2.UserName);
        user3 = await context.Users.FirstAsync(u => u.UserName == user3.UserName);
        user4 = await context.Users.FirstAsync(u => u.UserName == user4.UserName);
        user5 = await context.Users.FirstAsync(u => u.UserName == user5.UserName);
        user6 = await context.Users.FirstAsync(u => u.UserName == user6.UserName);
        user7 = await context.Users.FirstAsync(u => u.UserName == user7.UserName);
        user8 = await context.Users.FirstAsync(u => u.UserName == user8.UserName);

        if (!context.Tournaments.Any())
        {
            // Tournament 1: Open tournament without bracket
            var tournament1 = new Tournament
            {
                Name = "Spring Invitational",
                StartDate = DateTime.UtcNow.AddDays(7),
                Status = TournamentStatus.Open,
                OwnerId = user1.Id,
                Owner = user1,
                Participants = new List<TournamentParticipant>()
            };

            tournament1.Participants.Add(new TournamentParticipant { Tournament = tournament1, Participant = user1 });
            tournament1.Participants.Add(new TournamentParticipant { Tournament = tournament1, Participant = user2 });

            // Tournament 2: Open tournament with more participants
            var tournament2 = new Tournament
            {
                Name = "Summer Cup",
                StartDate = DateTime.UtcNow.AddDays(30),
                Status = TournamentStatus.Open,
                OwnerId = user2.Id,
                Owner = user2,
                Participants = new List<TournamentParticipant>()
            };

            tournament2.Participants.Add(new TournamentParticipant { Tournament = tournament2, Participant = user2 });
            tournament2.Participants.Add(new TournamentParticipant { Tournament = tournament2, Participant = user3 });

            // Tournament 3: Closed tournament with 8 participants and full bracket (completed)
            var tournament3 = new Tournament
            {
                Name = "Winter Championship 2024",
                StartDate = DateTime.UtcNow.AddDays(-30),
                Status = TournamentStatus.Closed,
                OwnerId = user1.Id,
                Owner = user1,
                Participants = new List<TournamentParticipant>(),
                Bracket = new Bracket
                {
                    Matches = new List<Match>()
                }
            };

            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user1 });
            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user2 });
            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user3 });
            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user4 });
            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user5 });
            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user6 });
            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user7 });
            tournament3.Participants.Add(new TournamentParticipant { Tournament = tournament3, Participant = user8 });

            // Round 1 - Quarter Finals (4 matches)
            var match1 = new Match { Round = 1, Player1Id = user1.Id, Player2Id = user2.Id, WinnerId = user1.Id, Bracket = tournament3.Bracket };
            var match2 = new Match { Round = 1, Player1Id = user3.Id, Player2Id = user4.Id, WinnerId = user4.Id, Bracket = tournament3.Bracket };
            var match3 = new Match { Round = 1, Player1Id = user5.Id, Player2Id = user6.Id, WinnerId = user5.Id, Bracket = tournament3.Bracket };
            var match4 = new Match { Round = 1, Player1Id = user7.Id, Player2Id = user8.Id, WinnerId = user7.Id, Bracket = tournament3.Bracket };

            // Round 2 - Semi Finals (2 matches)
            var match5 = new Match { Round = 2, Player1Id = user1.Id, Player2Id = user4.Id, WinnerId = user1.Id, Bracket = tournament3.Bracket };
            var match6 = new Match { Round = 2, Player1Id = user5.Id, Player2Id = user7.Id, WinnerId = user5.Id, Bracket = tournament3.Bracket };

            // Round 3 - Finals (1 match)
            var match7 = new Match { Round = 3, Player1Id = user1.Id, Player2Id = user5.Id, WinnerId = user1.Id, Bracket = tournament3.Bracket };

            tournament3.Bracket.Matches.Add(match1);
            tournament3.Bracket.Matches.Add(match2);
            tournament3.Bracket.Matches.Add(match3);
            tournament3.Bracket.Matches.Add(match4);
            tournament3.Bracket.Matches.Add(match5);
            tournament3.Bracket.Matches.Add(match6);
            tournament3.Bracket.Matches.Add(match7);

            // Tournament 4: Closed tournament with 4 participants and partial bracket (in progress)
            var tournament4 = new Tournament
            {
                Name = "Autumn Battle",
                StartDate = DateTime.UtcNow.AddDays(-10),
                Status = TournamentStatus.Closed,
                OwnerId = user3.Id,
                Owner = user3,
                Participants = new List<TournamentParticipant>(),
                Bracket = new Bracket
                {
                    Matches = new List<Match>()
                }
            };

            tournament4.Participants.Add(new TournamentParticipant { Tournament = tournament4, Participant = user2 });
            tournament4.Participants.Add(new TournamentParticipant { Tournament = tournament4, Participant = user3 });
            tournament4.Participants.Add(new TournamentParticipant { Tournament = tournament4, Participant = user5 });
            tournament4.Participants.Add(new TournamentParticipant { Tournament = tournament4, Participant = user6 });
            tournament4.Participants.Add(new TournamentParticipant { Tournament = tournament4, Participant = user7 });
            tournament4.Participants.Add(new TournamentParticipant { Tournament = tournament4, Participant = user8 });

            // Round 1 - Semi Finals (2 matches, one completed, one in progress)
            var match8 = new Match { Round = 1, Player1Id = user2.Id, Player2Id = user3.Id, WinnerId = user3.Id, Bracket = tournament4.Bracket };
            var match9 = new Match { Round = 1, Player1Id = user5.Id, Player2Id = user6.Id, WinnerId = null, Bracket = tournament4.Bracket };
            var match10 = new Match { Round = 1, Player1Id = user7.Id, Player2Id = user8.Id, WinnerId = null, Bracket = tournament4.Bracket };

            tournament4.Bracket.Matches.Add(match8);
            tournament4.Bracket.Matches.Add(match9);
            tournament4.Bracket.Matches.Add(match10);

            // Tournament 5: Closed tournament with odd number of participants (5 players)
            var tournament5 = new Tournament
            {
                Name = "Quick Fire Tournament",
                StartDate = DateTime.UtcNow.AddDays(-5),
                Status = TournamentStatus.Closed,
                OwnerId = user4.Id,
                Owner = user4,
                Participants = new List<TournamentParticipant>(),
                Bracket = new Bracket
                {
                    Matches = new List<Match>()
                }
            };

            tournament5.Participants.Add(new TournamentParticipant { Tournament = tournament5, Participant = user1 });
            tournament5.Participants.Add(new TournamentParticipant { Tournament = tournament5, Participant = user3 });
            tournament5.Participants.Add(new TournamentParticipant { Tournament = tournament5, Participant = user4 });
            tournament5.Participants.Add(new TournamentParticipant { Tournament = tournament5, Participant = user6 });
            tournament5.Participants.Add(new TournamentParticipant { Tournament = tournament5, Participant = user8 });

            // Round 1 - First round with bye (3 matches, one player gets bye)
            var match11 = new Match { Round = 1, Player1Id = user1.Id, Player2Id = user3.Id, WinnerId = user1.Id, Bracket = tournament5.Bracket };
            var match12 = new Match { Round = 1, Player1Id = user4.Id, Player2Id = user6.Id, WinnerId = user6.Id, Bracket = tournament5.Bracket };
            var match13 = new Match { Round = 1, Player1Id = user8.Id, Player2Id = null, WinnerId = user8.Id, Bracket = tournament5.Bracket }; // Bye

            tournament5.Bracket.Matches.Add(match11);
            tournament5.Bracket.Matches.Add(match12);
            tournament5.Bracket.Matches.Add(match13);

            // Tournament 6: Soft-deleted open tournament without bracket
            var tournament6 = new Tournament
            {
                Name = "Cancelled Spring Event",
                StartDate = DateTime.UtcNow.AddDays(14),
                Status = TournamentStatus.Open,
                OwnerId = user2.Id,
                Owner = user2,
                IsDeleted = true,
                Participants = new List<TournamentParticipant>()
            };

            tournament6.Participants.Add(new TournamentParticipant { Tournament = tournament6, Participant = user2, IsDeleted = true });
            tournament6.Participants.Add(new TournamentParticipant { Tournament = tournament6, Participant = user4, IsDeleted = true });
            tournament6.Participants.Add(new TournamentParticipant { Tournament = tournament6, Participant = user6, IsDeleted = true });

            // Tournament 7
            var tournament7 = new Tournament
            {
                Name = "Championship",
                StartDate = DateTime.UtcNow.AddDays(-15),
                Status = TournamentStatus.Closed,
                OwnerId = user3.Id,
                Owner = user3,
                Participants = new List<TournamentParticipant>(),
                Bracket = new Bracket
                {
                    Matches = new List<Match>()
                }
            };

            tournament7.Participants.Add(new TournamentParticipant { Tournament = tournament7, Participant = user1 });
            tournament7.Participants.Add(new TournamentParticipant { Tournament = tournament7, Participant = user2 });
            tournament7.Participants.Add(new TournamentParticipant { Tournament = tournament7, Participant = user7 });
            tournament7.Participants.Add(new TournamentParticipant { Tournament = tournament7, Participant = user8 });

            var match14 = new Match { Round = 1, Player1Id = user1.Id, Player2Id = user2.Id, WinnerId = user1.Id, Bracket = tournament7.Bracket };
            var match15 = new Match { Round = 1, Player1Id = user7.Id, Player2Id = user8.Id, WinnerId = user7.Id, Bracket = tournament7.Bracket };

            tournament7.Bracket.Matches.Add(match14);
            tournament7.Bracket.Matches.Add(match15);

            // Tournament 8: Closed Tournament with even number of participants
            var tournament8 = new Tournament
            {
                Name = "Event",
                StartDate = DateTime.UtcNow.AddDays(20),
                Status = TournamentStatus.Closed,
                OwnerId = user2.Id,
                Owner = user2,
                Participants = new List<TournamentParticipant>()
            };

            tournament8.Participants.Add(new TournamentParticipant { Tournament = tournament8, Participant = user3 });
            tournament8.Participants.Add(new TournamentParticipant { Tournament = tournament8, Participant = user5 });

            // Tournament 9: Closed tournament with no participants
            var tournament9 = new Tournament
            {
                Name = "Empty Tournament",
                StartDate = DateTime.UtcNow.AddDays(10),
                Status = TournamentStatus.Closed,
                OwnerId = user2.Id,
                Owner = user2,
                Participants = new List<TournamentParticipant>()
            };

            // Tournament 10: Closed tournament with odd number of participants (3 players)
            var tournament10 = new Tournament
            {
                Name = "Trio Tournament",
                StartDate = DateTime.UtcNow.AddDays(12),
                Status = TournamentStatus.Closed,
                OwnerId = user2.Id,
                Owner = user2,
                Participants = new List<TournamentParticipant>()
            };
            tournament10.Participants.Add(new TournamentParticipant { Tournament = tournament10, Participant = user2 });
            tournament10.Participants.Add(new TournamentParticipant { Tournament = tournament10, Participant = user4 });
            tournament10.Participants.Add(new TournamentParticipant { Tournament = tournament10, Participant = user6 });

            var tournament11 = new Tournament
            {
                Name = "Solo Tournament",
                StartDate = DateTime.UtcNow.AddDays(25),
                Status = TournamentStatus.Closed,
                OwnerId = user1.Id,
                Owner = user1,
                Participants = new List<TournamentParticipant>()
            };

            tournament11.Participants.Add(new TournamentParticipant { Tournament = tournament11, Participant = user5 });
            tournament11.Participants.Add(new TournamentParticipant { Tournament = tournament11, Participant = user7 });
            tournament11.Participants.Add(new TournamentParticipant { Tournament = tournament11, Participant = user8 });

            var tournament12 = new Tournament
            {
                Name = "Doubles Tournament",
                StartDate = DateTime.UtcNow.AddDays(18),
                Status = TournamentStatus.Closed,
                OwnerId = user1.Id,
                Owner = user1,
                Participants = new List<TournamentParticipant>(),
                Bracket = new Bracket
                {
                    Matches = new List<Match>()
                }
            };

            tournament12.Participants.Add(new TournamentParticipant { Tournament = tournament12, Participant = user1 });
            tournament12.Participants.Add(new TournamentParticipant { Tournament = tournament12, Participant = user2 });
            tournament12.Participants.Add(new TournamentParticipant { Tournament = tournament12, Participant = user3 });
            tournament12.Participants.Add(new TournamentParticipant { Tournament = tournament12, Participant = user4 });

            var match16 = new Match { Round = 1, Player1Id = user1.Id, Player2Id = user2.Id, WinnerId = user2.Id, Bracket = tournament12.Bracket };
            var match17 = new Match { Round = 1, Player1Id = user3.Id, Player2Id = user4.Id, WinnerId = user4.Id, Bracket = tournament12.Bracket };

            tournament12.Bracket.Matches.Add(match16);
            tournament12.Bracket.Matches.Add(match17);

            await context.Tournaments.AddRangeAsync(tournament1, tournament2, tournament3, tournament4, tournament5, tournament6, tournament7, tournament8, tournament9, tournament10, tournament11, tournament12);
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
