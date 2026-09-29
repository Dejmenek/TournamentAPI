using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;

namespace TournamentAPI.LoadTests;

internal sealed record ProvisionedMatch(int Id, int Player1Id, int Player2Id, string Version);

internal static partial class WriteScenarioProvisioner
{
    public const int PlayedMatchPlayer1Score = 1;

    public static async Task<IReadOnlyList<ProvisionedMatch>> CreateClosedTournamentMatchesAsync(
        IServiceProvider services,
        string ownerEmail,
        IReadOnlyList<string> playerEmails,
        string namePrefix,
        int matchCount,
        bool played)
    {
        if (playerEmails.Count < 2 || playerEmails.Count % 2 != 0)
        {
            throw new ArgumentException("An even number of at least two players is required.", nameof(playerEmails));
        }

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = await context.Users.SingleAsync(u => u.Email == ownerEmail);
        var players = await context.Users
            .Where(u => playerEmails.Contains(u.Email!))
            .OrderBy(u => u.Id)
            .ToListAsync();

        var matchesPerTournament = players.Count / 2;
        var tournamentCount = (matchCount + matchesPerTournament - 1) / matchesPerTournament;
        var matches = new List<Match>();

        for (var i = 0; i < tournamentCount; i++)
        {
            var tournament = new Tournament
            {
                Name = $"{namePrefix} {i}",
                StartDate = DateTime.UtcNow.AddDays(7),
                OwnerId = owner.Id,
                MaxParticipants = players.Count,
                Status = TournamentStatus.Closed,
                Format = TournamentFormat.SingleElimination,
                Participants = players
                    .Select((player, slot) => new TournamentParticipant { ParticipantId = player.Id, SlotNumber = slot + 1 })
                    .ToList(),
                Bracket = new Bracket { Matches = new List<Match>() }
            };

            for (var pair = 0; pair < matchesPerTournament; pair++)
            {
                var match = new Match
                {
                    Round = 1,
                    Player1Id = players[pair * 2].Id,
                    Player2Id = players[pair * 2 + 1].Id,
                    Status = played ? MatchStatus.Played : MatchStatus.Scheduled,
                    WinnerId = played ? players[pair * 2].Id : null,
                    Player1Score = played ? PlayedMatchPlayer1Score : 0,
                    Player2Score = 0
                };

                tournament.Bracket.Matches.Add(match);
                matches.Add(match);
            }

            context.Tournaments.Add(tournament);
        }

        await context.SaveChangesAsync();

        return matches
            .Take(matchCount)
            .Select(m => new ProvisionedMatch(m.Id, m.Player1Id, m.Player2Id!.Value, MatchVersionCodec.Encode(m.RowVersion)))
            .ToList();
    }
}
