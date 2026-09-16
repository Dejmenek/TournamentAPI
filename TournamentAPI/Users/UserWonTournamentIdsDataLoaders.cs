using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

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
        return await context.Tournaments
            .Where(t => t.Status == TournamentStatus.Completed && t.ChampionId.HasValue && winnerIds.Contains(t.ChampionId.Value))
            .GroupBy(t => t.ChampionId!.Value)
            .Select(g => new { g.Key, TournamentIds = g.Select(t => t.Id).ToArray() })
            .ToDictionaryAsync(g => g.Key, g => g.TournamentIds, cancellationToken);
    }
}
