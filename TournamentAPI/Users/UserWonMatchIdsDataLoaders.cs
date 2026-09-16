using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

[DataLoaderGroup("UserWonMatchIdsBatchingContext")]
internal static class UserWonMatchIdsDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, int[]>> GetWonMatchIdsByWinnerIdAsync(
        IReadOnlyList<int> winnerIds,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        winnerIds = [.. winnerIds.OrderBy(x => x)];
        return await context.Matches
            .Where(m => m.WinnerId.HasValue && winnerIds.Contains(m.WinnerId.Value) && m.Status == MatchStatus.Played)
            .GroupBy(m => m.WinnerId!.Value)
            .Select(g => new { g.Key, MatchIds = g.Select(m => m.Id).ToArray() })
            .ToDictionaryAsync(g => g.Key, g => g.MatchIds, cancellationToken);
    }
}
