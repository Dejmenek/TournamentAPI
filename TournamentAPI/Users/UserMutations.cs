using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
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
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IResolverContext resolverContext,
        JwtService jwtService)
    {
        var user = await userManager.FindByEmailAsync(input.Email);

        if (resolverContext.TryReportError(UserValidations.ValidateCredentials(user)))
            return null;

        var canSignIn = await signInManager.CheckPasswordSignInAsync(user!, input.Password, true);

        if (canSignIn.IsLockedOut)
        {
            resolverContext.ReportError(UserErrors.AccountLockedOut);
            return null;
        }

        if (!canSignIn.Succeeded)
        {
            resolverContext.ReportError(UserErrors.InvalidCredentials());
            return null;
        }

        var accessToken = jwtService.CreateToken(user);
        var refreshTokenResult = jwtService.CreateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenResult.Hashed,
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

        httpContextAccessor.HttpContext.Response.AppendRefreshTokenCookie(refreshTokenResult.Raw, refreshToken.ExpiryDateUtc);

        return accessToken;
    }

    public async Task<string?> RefreshToken(
        JwtService jwtService,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        IHttpContextAccessor httpContextAccessor
    )
    {
        if (httpContextAccessor.HttpContext == null)
        {
            resolverContext.ReportError(UserErrors.UnableToSetRefreshTokenCookie());
            return null;
        }

        var rawCookieToken = httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
        var hashedCookieToken = jwtService.HashRefreshToken(rawCookieToken ?? string.Empty);

        var refreshTokenEntity = await context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == hashedCookieToken);

        if (refreshTokenEntity is null)
        {
            resolverContext.ReportError(UserErrors.RefreshTokenInvalid());
            return null;
        }

        if (refreshTokenEntity.ExpiryDateUtc < DateTime.UtcNow)
        {
            resolverContext.ReportError(UserErrors.RefreshTokenExpired());
            return null;
        }

        string accessToken = jwtService.CreateToken(refreshTokenEntity.User);
        var newRefreshToken = jwtService.CreateRefreshToken();
        refreshTokenEntity.Token = newRefreshToken.Hashed;
        refreshTokenEntity.ExpiryDateUtc = DateTime.UtcNow.AddDays(7);

        await context.SaveChangesAsync();

        httpContextAccessor.HttpContext.Response.AppendRefreshTokenCookie(newRefreshToken.Raw, refreshTokenEntity.ExpiryDateUtc);

        return accessToken;
    }
}
