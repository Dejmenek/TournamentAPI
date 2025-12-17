using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public class TournamentByIdDataLoader : BatchDataLoader<int, Tournament>
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public TournamentByIdDataLoader(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) : base(batchScheduler, options)
    {
        _contextFactory = contextFactory;
    }

    protected override async Task<IReadOnlyDictionary<int, Tournament>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => keys.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}