using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Matches;

public class MatchesByBracketIdDataLoader : BatchDataLoader<int, List<Match>>
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public MatchesByBracketIdDataLoader(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) : base(batchScheduler, options)
    {
        _contextFactory = contextFactory;
    }

    protected override async Task<IReadOnlyDictionary<int, List<Match>>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();
        var matches = await context.Matches
            .Where(m => keys.Contains(m.BracketId))
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .ToListAsync(cancellationToken);

        return matches
            .GroupBy(m => m.BracketId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
