using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.LoadTests;

internal static partial class WriteScenarioProvisioner
{
    public static async Task<IReadOnlyList<int>> CreateClosedTournamentsAsync(
        IServiceProvider services,
        string ownerEmail,
        IReadOnlyList<string> playerEmails,
        string namePrefix,
        int count,
        int participantCount,
        TournamentFormat format)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = await context.Users.SingleAsync(u => u.Email == ownerEmail);
        var players = await context.Users
            .Where(u => playerEmails.Contains(u.Email!))
            .OrderBy(u => u.Id)
            .Take(participantCount)
            .ToListAsync();

        if (players.Count < participantCount)
        {
            throw new ArgumentException($"Only {players.Count} of the {participantCount} required players exist.", nameof(playerEmails));
        }

        var tournaments = new List<Tournament>(count);

        for (var i = 0; i < count; i++)
        {
            tournaments.Add(new Tournament
            {
                Name = $"{namePrefix} {i}",
                StartDate = DateTime.UtcNow.AddDays(7),
                OwnerId = owner.Id,
                MaxParticipants = participantCount,
                Status = TournamentStatus.Closed,
                Format = format,
                Participants = players
                    .Select((player, slot) => new TournamentParticipant { ParticipantId = player.Id, SlotNumber = slot + 1 })
                    .ToList()
            });
        }

        context.Tournaments.AddRange(tournaments);
        await context.SaveChangesAsync();

        return tournaments.Select(t => t.Id).ToList();
    }
}
