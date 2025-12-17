using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Matches;

[ExtendObjectType(typeof(Query))]
public class MatchQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Match> GetMatchesForRound(int tournamentId, int roundNumber, [Service] ApplicationDbContext context)
    {
        return context.Tournaments
            .AsNoTracking()
            .Where(t => t.Id == tournamentId && t.Bracket != null)
            .SelectMany(t => t.Bracket!.Matches)
            .Where(m => m.Round == roundNumber);
    }
}
