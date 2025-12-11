using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
    public IQueryable<Tournament> GetTournaments([Service] ApplicationDbContext context)
    {
        return context.Tournaments;
    }

    [UseProjection]
    public async Task<Tournament?> GetTournamentByIdAsync(int id, [Service] ApplicationDbContext context)
    {
        return await context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Match> GetMatchesForRound(int tournamentId, int roundNumber, [Service] ApplicationDbContext context)
    {
        return context.Tournaments
            .Where(t => t.Id == tournamentId && t.Bracket != null)
            .SelectMany(t => t.Bracket!.Matches)
            .Where(m => m.Round == roundNumber);
    }
}
