using TournamentAPI.Data.Models;

namespace TournamentAPI.Standings;

public class StandingsLookupService(IStandingsBatchingContext batchingContext)
{
    public async Task<IReadOnlyList<Match>> GetMatchesByBracketIdAsync(
        int bracketId,
        CancellationToken cancellationToken = default
    )
    {
        return await batchingContext.MatchesByBracketId.LoadAsync(bracketId, cancellationToken) ?? [];
    }
}
