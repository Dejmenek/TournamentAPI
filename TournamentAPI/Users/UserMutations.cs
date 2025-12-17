using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Services;

namespace TournamentAPI.Users;

[ExtendObjectType(typeof(Mutation))]
public class UserMutations
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public UserMutations(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> RegisterUser(RegisterUserInput input, [Service] UserManager<ApplicationUser> userManager)
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
        [Service] UserManager<ApplicationUser> userManager,
        [Service] JwtService jwtService)
    {
        var user = await userManager.FindByEmailAsync(input.Email)
            ?? throw new GraphQLException("Invalid username or password.");

        if (!await userManager.CheckPasswordAsync(user, input.Password))
            throw new GraphQLException("Invalid username or password.");

        var token = jwtService.CreateToken(user);
        return token;
    }
}
