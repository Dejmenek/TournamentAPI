using GreenDonut.Data;
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
    public IQueryable<Tournament> GetTournaments(ApplicationDbContext context)
    {
        return context.Tournaments.AsNoTracking().Where(t => !t.IsDeleted);
    }

    [UseProjection]
    public async Task<Tournament?> GetTournamentByIdAsync(
        int id, TournamentByIdDataLoader tournamentById, CancellationToken token)
    {
        return await tournamentById.LoadAsync(id, token);
    }
}
