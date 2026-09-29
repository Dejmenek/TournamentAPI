using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.LoadTests;

internal static partial class WriteScenarioProvisioner
{
    public static async Task<IReadOnlyList<int>> CreateOpenTournamentsAsync(
        IServiceProvider services,
        string ownerEmail,
        string namePrefix,
        int count,
        int maxParticipants)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = await context.Users.SingleAsync(u => u.Email == ownerEmail);
        var tournaments = new List<Tournament>(count);

        for (var i = 0; i < count; i++)
        {
            var tournament = new Tournament
            {
                Name = $"{namePrefix} {i}",
                StartDate = DateTime.UtcNow.AddDays(7),
                OwnerId = owner.Id,
                MaxParticipants = maxParticipants,
                Status = TournamentStatus.Open,
                Format = TournamentFormat.SingleElimination,
                Participants = new List<TournamentParticipant>
                {
                    new() { ParticipantId = owner.Id, SlotNumber = 1 }
                }
            };

            tournaments.Add(tournament);
        }

        context.Tournaments.AddRange(tournaments);
        await context.SaveChangesAsync();

        return tournaments.Select(t => t.Id).ToList();
    }
}
