using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

[DataLoaderGroup("TournamentBatchingContext")]
internal static class TournamentDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Tournament>> GetTournamentByIdAsync(
        IReadOnlyList<int> ids,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        ids = [.. ids.OrderBy(x => x)];
        return await context.Tournaments
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}
