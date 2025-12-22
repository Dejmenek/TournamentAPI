using GreenDonut.Data;
using HotChocolate.Data.Sorting;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

[ExtendObjectType(typeof(Query))]
public class TournamentQueries
{
    [UsePaging(
        MaxPageSize = 100,
        IncludeTotalCount = true,
        DefaultPageSize = 10,
        RequirePagingBoundaries = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Tournament> GetTournaments(
        ISortingContext sorting, ApplicationDbContext context)
    {
        sorting.Handled(false);

        sorting.OnAfterSortingApplied<IQueryable<Tournament>>(
            static (sortingApplied, query) =>
            {
                if (sortingApplied && query is IOrderedQueryable<Tournament> ordered)
                {
                    return ordered.ThenBy(t => t.Id);
                }

                return query.OrderBy(t => t.Id);
            }
        );

        return context.Tournaments.AsNoTracking();
    }

    [UseProjection]
    public async Task<Tournament?> GetTournamentByIdAsync(
        int id,
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        return await context.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
