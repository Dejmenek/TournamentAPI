using GreenDonut.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Matches;

public class MatchService(IMatchBatchingContext batchingContext)
{
    public async Task<Page<Match>> GetMatchesByBracketAsync(
        int bracketId,
        PagingArguments pagingArgs,
        QueryContext<Match>? query = null,
        CancellationToken cancellationToken = default
    )
    {
        return await batchingContext.MatchesByBracket
            .With(pagingArgs, query)
            .LoadAsync(bracketId, cancellationToken)
            ?? Page<Match>.Empty;
    }
}
