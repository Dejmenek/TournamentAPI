using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Metrics;
using TournamentAPI.Services;

namespace TournamentAPI.Users;

[MutationType]
public static partial class UserMutations
{
    [Authorize]
    public static async Task<ApplicationUser?> UpdateEmailVisibility(
        UpdateEmailVisibilityInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, token);

        if (resolverContext.TryReportError(UserValidations.ValidateUserExists(user, userId)))
            return null;

        user!.IsEmailPublic = input.IsEmailPublic;

        await context.SaveChangesAsync(token);

        return user;
    }

    public static async Task<bool?> RegisterUser(
        RegisterUserInput input,
        UserManager<ApplicationUser> userManager,
        IResolverContext resolverContext,
        UserMetrics userMetrics)
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

        userMetrics.UserRegistered();
        return true;
    }

    [Authorize]
    public static async Task<bool?> LogoutUser(
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        IHttpContextAccessor httpContextAccessor,
        JwtService jwtService
    )
    {
        await signInManager.SignOutAsync();
        if (httpContextAccessor.HttpContext == null)
        {
            resolverContext.ReportError(UserErrors.HttpContextUnavailable());
            return null;
        }

        var rawCookieToken = httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
        var hashedCookieToken = jwtService.HashRefreshToken(rawCookieToken ?? string.Empty);

        var existingToken = await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == hashedCookieToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            resolverContext.ReportError(UserErrors.RefreshTokenInvalid());
            return null;
        }

        existingToken.Revoked = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await context.Entry(existingToken).ReloadAsync();

            if (existingToken.IsActive)
            {
                resolverContext.ReportError(UserErrors.RefreshTokenConflict());
                return null;
            }
        }

        httpContextAccessor.HttpContext.Response.ClearRefreshTokenCookie();

        return true;
    }

    public static async Task<string?> LoginUser(
        LoginUserInput input,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IResolverContext resolverContext,
        JwtService jwtService,
        ILoggerFactory loggerFactory,
        UserMetrics userMetrics)
    {
        var logger = loggerFactory.CreateLogger(typeof(UserMutations).FullName!);

        var user = await userManager.FindByEmailAsync(input.Email);

        if (resolverContext.TryReportError(UserValidations.ValidateCredentials(user)))
        {
            userMetrics.LoginFailed();
            logger.LogWarning("Login failed: no account found for the given email");
            return null;

        var canSignIn = await signInManager.CheckPasswordSignInAsync(user!, input.Password, true);

        if (canSignIn.IsLockedOut)
        {
            userMetrics.LoginFailed();
            logger.LogWarning("Login failed: account {UserId} is locked out", user.Id);
            resolverContext.ReportError(UserErrors.AccountLockedOut);
            return null;
        }

        if (!canSignIn.Succeeded)
        {
            userMetrics.LoginFailed();
            logger.LogWarning("Login failed: invalid credentials for account {UserId}", user.Id);
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
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
        };

        if (httpContextAccessor.HttpContext == null)
        {
            resolverContext.ReportError(UserErrors.UnableToSetRefreshTokenCookie());
            return null;
        }

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        userMetrics.LoginSucceeded();

        httpContextAccessor.HttpContext.Response.AppendRefreshTokenCookie(refreshTokenResult.Raw, refreshToken.Expires);

        return accessToken;
    }

    public static async Task<string?> RefreshToken(
        JwtService jwtService,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory,
        UserMetrics userMetrics
    )
    {
        var logger = loggerFactory.CreateLogger(typeof(UserMutations).FullName!);

        if (httpContextAccessor.HttpContext == null)
        {
            userMetrics.RefreshTokenFailed("missing_cookie");
            resolverContext.ReportError(UserErrors.UnableToSetRefreshTokenCookie());
            return null;
        }

        var rawCookieToken = httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];
        var hashedCookieToken = jwtService.HashRefreshToken(rawCookieToken ?? string.Empty);

        var existingToken = await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == hashedCookieToken);

        if (existingToken is null)
        {
            userMetrics.RefreshTokenFailed("not_found");
            resolverContext.ReportError(UserErrors.RefreshTokenInvalid());
            return null;
        }

        if (!existingToken.IsActive)
        {
            if (existingToken.Revoked is not null)
            {
                await RevokeAllActiveTokensAsync(context, existingToken.UserId);
                userMetrics.RefreshTokenFailed("reused");
                logger.LogWarning(
                    "Refresh token theft detected for user {UserId}: a revoked token was reused, all active tokens have been revoked",
                    existingToken.UserId);
                resolverContext.ReportError(UserErrors.RefreshTokenReused());
                return null;
            }

            userMetrics.RefreshTokenFailed("expired");
            resolverContext.ReportError(UserErrors.RefreshTokenExpired());
            return null;
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == existingToken.UserId);
        if (user is null)
        {
            userMetrics.RefreshTokenFailed("user_not_found");
            resolverContext.ReportError(UserErrors.UserNotFound(existingToken.UserId));
            return null;
        }

        var newRefreshToken = jwtService.CreateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(7);

        existingToken.Revoked = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshToken.Hashed;

        context.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken.Hashed,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = refreshExpiresAt
        });

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            userMetrics.RefreshTokenFailed("conflict");
            resolverContext.ReportError(UserErrors.RefreshTokenConflict());
            return null;
        }

        string accessToken = jwtService.CreateToken(user);
        httpContextAccessor.HttpContext.Response.AppendRefreshTokenCookie(newRefreshToken.Raw, refreshExpiresAt);

        return accessToken;
    }

    private static async Task RevokeAllActiveTokensAsync(ApplicationDbContext context, int userId)
    {
        var activeTokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.Revoked == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.Revoked = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }
}
