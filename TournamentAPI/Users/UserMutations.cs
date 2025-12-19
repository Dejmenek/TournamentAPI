using Microsoft.AspNetCore.Identity;
using TournamentAPI.Data.Models;
using TournamentAPI.Services;

namespace TournamentAPI.Users;

[ExtendObjectType(typeof(Mutation))]
public class UserMutations
{
    public async Task<bool> RegisterUser(RegisterUserInput input, UserManager<ApplicationUser> userManager)
    {
        var user = new ApplicationUser
        {
            UserName = input.UserName,
            Email = input.Email
        };

        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new GraphQLException($"User registration failed: {errors}");
        }

        return true;
    }

    public async Task<string> LoginUser(
        LoginUserInput input,
        UserManager<ApplicationUser> userManager,
        JwtService jwtService)
    {
        var user = await userManager.FindByEmailAsync(input.Email)
            ?? throw new GraphQLException("Invalid username or password.");

        if (!await userManager.CheckPasswordAsync(user, input.Password))
            throw new GraphQLException("Invalid username or password.");

        var token = jwtService.CreateToken(user);
        return token;
    }
}
