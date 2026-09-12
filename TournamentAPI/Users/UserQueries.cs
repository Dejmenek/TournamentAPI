using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;

namespace TournamentAPI.Users;

[QueryType]
public static partial class UserQueries
{
    [Authorize]
    public static async Task<ApplicationUser?> GetMe(
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        CancellationToken token)
    {
        var userId = claimsPrincipal.GetUserId();

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, token);

        if (resolverContext.TryReportError(UserValidations.ValidateUserExists(user, userId)))
            return null;

        return user;
    }
}
