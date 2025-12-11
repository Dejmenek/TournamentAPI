using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Models;

namespace TournamentAPI.DataLoaders;

public class ParticipantsByTournamentIdDataLoader : BatchDataLoader<int, List<TournamentParticipant>>
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ParticipantsByTournamentIdDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options,
        IDbContextFactory<ApplicationDbContext> contextFactory) : base(batchScheduler, options)
    {
        _contextFactory = contextFactory;
    }

    protected override async Task<IReadOnlyDictionary<int, List<TournamentParticipant>>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();
        var participants = await context.TournamentParticipants
            .Where(tp => keys.Contains(tp.TournamentId))
            .Include(tp => tp.Participant)
            .ToListAsync(cancellationToken);

        return participants
            .GroupBy(tp => tp.TournamentId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
