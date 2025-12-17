using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

[ExtendObjectType(typeof(Query))]
public class UserQueries
{
    [Authorize]
    public async Task<ApplicationUser> GetMe(
        ClaimsPrincipal claimsPrincipal, [Service] ApplicationDbContext context, CancellationToken token)
    {
        var userIdClaim = (claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier))
            ?? throw new GraphQLException("User is not authenticated.");

        var userId = int.Parse(userIdClaim);
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, token);

        return user ?? throw new GraphQLException("User not found.");
    }
}
