using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;

namespace TournamentAPI.Users;

[DataLoaderGroup("UserWonTournamentIdsBatchingContext")]
internal static class UserWonTournamentIdsDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, int[]>> GetWonTournamentIdsByWinnerIdAsync(
        IReadOnlyList<int> winnerIds,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        winnerIds = [.. winnerIds.OrderBy(x => x)];
        return await context.Matches
            .Where(m => m.WinnerId.HasValue && winnerIds.Contains(m.WinnerId.Value))
            .Where(m => m.Round == m.Bracket.Matches.Max(x => x.Round)
                     && m.Bracket.Matches.Count(o => o.Round == m.Round) == 1)
            .GroupBy(m => m.WinnerId!.Value)
            .Select(g => new { g.Key, TournamentIds = g.Select(m => m.Bracket.TournamentId).ToArray() })
            .ToDictionaryAsync(g => g.Key, g => g.TournamentIds, cancellationToken);
    }
}
