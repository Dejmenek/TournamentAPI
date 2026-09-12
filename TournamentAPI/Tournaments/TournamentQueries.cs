using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;

namespace TournamentAPI.Tournaments;

[QueryType]
public static partial class TournamentQueries
{
    [UseConnection(
        MaxPageSize = 100,
        IncludeTotalCount = true,
        DefaultPageSize = 10,
        RequirePagingBoundaries = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Tournament>> GetTournaments(
        PagingArguments pagingArgs,
        QueryContext<Tournament> query,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var page = await context.Tournaments
            .AsNoTracking()
            .With(query, s => s.DefaultTournamentOrder())
            .ToPageAsync(pagingArgs, cancellationToken);

        return page;
    }

    [UseFirstOrDefault]
    public static IQueryable<Tournament>? GetTournamentById(
        int id,
        QueryContext<Tournament> query,
        ApplicationDbContext context)
    {
        return context.Tournaments
            .AsNoTracking()
            .Where(t => t.Id == id)
            .With(query);
    }
}
