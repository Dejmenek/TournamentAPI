using GreenDonut.Data;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Matches;

[DataLoaderGroup("MatchBatchingContext")]
internal static class MatchDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Page<Match>>> GetMatchesByBracketAsync(
        IReadOnlyList<int> bracketIds,
        PagingArguments pagingArgs,
        QueryContext<Match> query,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        bracketIds = [.. bracketIds.OrderBy(x => x)];
        return await context.Matches
            .Where(m => bracketIds.Contains(m.BracketId))
            .With(query, DefaultOrder)
            .ToBatchPageAsync(
                m => m.BracketId,
                pagingArgs,
                cancellationToken
            );
    }
    private static SortDefinition<Match> DefaultOrder(SortDefinition<Match> sort)
    {
        return sort.IfEmpty(m => m.AddAscending(m => m.Round)).AddAscending(m => m.Id);
    }
}
