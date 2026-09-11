using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

[DataLoaderGroup("ApplicationUserBatchingContext")]
internal static class ApplicationUserDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, ApplicationUser>> GetApplicationUserByIdAsync(
        IReadOnlyList<int> ids,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        ids = [.. ids.OrderBy(x => x)];
        return await context.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }
}
