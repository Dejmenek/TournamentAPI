using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Brackets;

[DataLoaderGroup("BracketBatchingContext")]
internal static class BracketDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Bracket>> GetBracketByTournamentIdAsync(
        IReadOnlyList<int> tournamentIds,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        tournamentIds = [.. tournamentIds.OrderBy(x => x)];
        return await context.Brackets
            .Where(b => tournamentIds.Contains(b.TournamentId))
            .ToDictionaryAsync(b => b.TournamentId, cancellationToken);
    }
}
