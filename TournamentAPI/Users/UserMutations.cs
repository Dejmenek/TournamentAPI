using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Identity;
using TournamentAPI.Data.Models;
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
        IResolverContext resolverContext,
        JwtService jwtService)
    {
        var user = await userManager.FindByEmailAsync(input.Email);

        if (user == null)
        {
            resolverContext.ReportError(UserErrors.InvalidCredentials());
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, input.Password))
        {
            resolverContext.ReportError(UserErrors.InvalidCredentials());
            return null;
        }

        var token = jwtService.CreateToken(user);
        return token;
    }
}
