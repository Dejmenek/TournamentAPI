using TournamentAPI.Data.Models;

namespace TournamentAPI.Brackets;

public class BracketLookupService(IBracketBatchingContext batchingContext)
{
    public async Task<Bracket?> GetBracketByTournamentIdAsync(
        int tournamentId,
        CancellationToken cancellationToken = default
    )
    {
        return await batchingContext.BracketByTournamentId.LoadAsync(tournamentId, cancellationToken);
    }
}
