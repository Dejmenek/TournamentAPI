using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Identity;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Services;

namespace TournamentAPI.Users;

[ExtendObjectType(typeof(Mutation))]
public class UserMutations
{
    public async Task<bool?> RegisterUser(
        RegisterUserInput input,
        UserManager<ApplicationUser> userManager,
        IResolverContext resolverContext)
    {
        var user = new ApplicationUser
        {
            UserName = input.UserName,
            Email = input.Email
        };

        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            resolverContext.ReportError(UserErrors.RegistrationFailed(result.Errors.Select(e => e.Description).ToArray()));
            return null;
        }

        return true;
    }

    public async Task<string?> LoginUser(
        LoginUserInput input,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IResolverContext resolverContext,
        JwtService jwtService)
    {
        var user = await userManager.FindByEmailAsync(input.Email);

        if (resolverContext.TryReportError(UserValidations.ValidateCredentials(user)))
            return null;

        if (!await userManager.CheckPasswordAsync(user, input.Password))
        {
            resolverContext.ReportError(UserErrors.InvalidCredentials());
            return null;
        }

        var accessToken = jwtService.CreateToken(user);
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = jwtService.CreateRefreshToken(),
            ExpiryDateUtc = DateTime.UtcNow.AddDays(7),
        };

        if (httpContextAccessor.HttpContext == null)
        {
            resolverContext.ReportError(UserErrors.UnableToSetRefreshTokenCookie());
            return null;
        }

        await context.RefreshTokens
            .Where(r => r.UserId == user.Id)
            .ExecuteDeleteAsync();

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        httpContextAccessor.HttpContext.Response.AppendRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiryDateUtc);

        return accessToken;
    }
        if (httpContextAccessor.HttpContext == null)
        {
            resolverContext.ReportError(UserErrors.UnableToSetRefreshTokenCookie());
            return null;
        }

        var token = jwtService.CreateToken(user);
        return token;
    }
}
