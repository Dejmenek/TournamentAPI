using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public class TournamentLookupService(ITournamentBatchingContext batchingContext)
{
    public async Task<Tournament?> GetTournamentByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        return await batchingContext.TournamentById.LoadAsync(id, cancellationToken);
    }
}
