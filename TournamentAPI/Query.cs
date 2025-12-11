using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Models;

namespace TournamentAPI;

public class Query
{
    [Authorize]
    public async Task<ApplicationUser> GetMe(ClaimsPrincipal claimsPrincipal, [Service] ApplicationDbContext context)
    {
        var userIdClaim = (claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier))
            ?? throw new GraphQLException("User is not authenticated.");
        var userId = int.Parse(userIdClaim);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user ?? throw new GraphQLException("User not found.");
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
