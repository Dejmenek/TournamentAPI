using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;

namespace TournamentAPI.Brackets;

[ObjectType<Bracket>]
public static partial class BracketResolvers
{
    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Match>> GetMatchesByBracket(
        [Parent(requires: nameof(Bracket.Id))] Bracket bracket,
        PagingArguments pagingArgs,
        QueryContext<Match> query,
        MatchService matchService,
        CancellationToken cancellationToken)
    {
        var page = await matchService.GetMatchesByBracketAsync(bracket.Id, pagingArgs, query, cancellationToken);
        return new PageConnection<Match>(page);
    }
}
