using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Standings;

[DataLoaderGroup("StandingsBatchingContext")]
internal static class StandingsDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, IReadOnlyList<Match>>> GetMatchesByBracketIdAsync(
        IReadOnlyList<int> bracketIds,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        bracketIds = [.. bracketIds.OrderBy(x => x)];

        var matches = await context.Matches
            .AsNoTracking()
            .Where(m => bracketIds.Contains(m.BracketId))
            .ToListAsync(cancellationToken);

        return matches
            .GroupBy(m => m.BracketId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Match>)g.ToList());
    }
}
