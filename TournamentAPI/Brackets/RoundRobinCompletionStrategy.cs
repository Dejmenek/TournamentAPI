using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Standings;

namespace TournamentAPI.Brackets;

public class RoundRobinCompletionStrategy(StandingsService standingsService) : IBracketCompletionStrategy
{
    public TournamentFormat Format => TournamentFormat.RoundRobin;

    public async Task SyncCompletionAsync(
        ApplicationDbContext context,
        Tournament tournament,
        int bracketId,
        int frontierRound,
        CancellationToken token)
    {
        var matches = await context.Matches
            .Where(m => m.BracketId == bracketId)
            .ToListAsync(token);

        var isComplete = matches.All(m => m.Status != MatchStatus.Scheduled);

        if (!isComplete)
            return;

        var standings = standingsService.ComputeStandings(matches);
        var leaders = standings.Where(s => s.Rank == 1).ToList();

        tournament.Status = TournamentStatus.Completed;
        tournament.ChampionId = leaders.Count == 1 ? leaders[0].ParticipantId : null;
    }
}
