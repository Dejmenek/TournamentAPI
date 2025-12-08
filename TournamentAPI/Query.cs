using TournamentAPI.Data;
using TournamentAPI.Models;

namespace TournamentAPI;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [UsePaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<Tournament> GetTournaments([Service] ApplicationDbContext context) =>
        context.Tournaments;

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Match> GetMatchesForRound(int tournamentId, int roundNumber, [Service] ApplicationDbContext context) =>
        context.Tournaments
            .Where(t => t.Id == tournamentId)
            .SelectMany(t => t.Bracket.Matches)
            .Where(m => m.Round == roundNumber);
}
