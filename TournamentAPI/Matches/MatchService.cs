using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Users;

namespace TournamentAPI.Matches;

public class MatchService(
    IMatchBatchingContext batchingContext,
    IUserWonMatchIdsBatchingContext wonMatchIdsBatchingContext,
    ApplicationDbContext context)
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

    public async Task<PageConnection<Match>> GetWonMatchesByUserAsync(
        int userId,
        PagingArguments pagingArgs,
        QueryContext<Match>? query,
        CancellationToken cancellationToken)
    {
        var ids = await wonMatchIdsBatchingContext.WonMatchIdsByWinnerId.LoadAsync(userId, cancellationToken) ?? [];

        var page = await context.Matches
            .Where(m => ids.Contains(m.Id))
            .With(query, s => s.DefaultMatchOrder())
            .ToPageAsync(pagingArgs, cancellationToken);

        return new PageConnection<Match>(page);
    }
}
