using TournamentAPI.Data;
using TournamentAPI.Models;

namespace TournamentAPI;

public class Query
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public Query(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [UsePaging(MaxPageSize = 100, IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Tournament> GetTournaments()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Tournaments;
    }

    [UseProjection]
    public Task<Tournament?> GetTournamentByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();

        return context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Match> GetMatchesForRound(int tournamentId, int roundNumber)
    {
        using var context = _contextFactory.CreateDbContext();

        return context.Tournaments
            .Where(t => t.Id == tournamentId && t.Bracket != null)
            .SelectMany(t => t.Bracket!.Matches)
            .Where(m => m.Round == roundNumber);
}
}
