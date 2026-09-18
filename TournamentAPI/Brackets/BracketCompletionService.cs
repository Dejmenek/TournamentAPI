using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Brackets;

public class BracketCompletionService(ILogger<BracketCompletionService> logger)
{
    public async Task SyncChampionAsync(
        ApplicationDbContext context,
        Tournament tournament,
        int bracketId,
        int frontierRound,
        CancellationToken token)
    {
        var hasLaterRound = await context.Matches
            .AnyAsync(m => m.BracketId == bracketId && m.Round > frontierRound, token);

        if (hasLaterRound)
            return;

        var finalRoundMatches = await context.Matches
            .Where(m => m.BracketId == bracketId && m.Round == frontierRound)
            .ToListAsync(token);

        if (finalRoundMatches.Count != 1)
            return;

        var finalMatch = finalRoundMatches[0];

        if (finalMatch.Status == MatchStatus.Played)
        {
            tournament.Status = TournamentStatus.Completed;
            tournament.ChampionId = finalMatch.WinnerId;

            logger.LogInformation(
                "Bracket {BracketId} marked complete for tournament {TournamentId}: champion is participant {ChampionId}",
                bracketId,
                tournament.Id,
                finalMatch.WinnerId);
        }
        else if (tournament.Status == TournamentStatus.Completed)
        {
            tournament.Status = TournamentStatus.Closed;
            tournament.ChampionId = null;
        }
    }
}
