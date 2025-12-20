using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

public class OwnerByTournamentIdDataLoader : BatchDataLoader<int, ApplicationUser>
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    public OwnerByTournamentIdDataLoader(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) : base(batchScheduler, options)
    {
        _contextFactory = contextFactory;
    }

    protected override async Task<IReadOnlyDictionary<int, ApplicationUser>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();

        return await context.Users
            .AsNoTracking()
            .Where(u => keys.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }
}
