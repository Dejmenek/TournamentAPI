using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Data.Models;

namespace TournamentAPI.LoadTests;

internal static partial class WriteScenarioProvisioner
{
    public static async Task<IReadOnlyList<string>> CreateUsersAsync(
        IServiceProvider services,
        string prefix,
        int count,
        string password)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var emails = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var user = new ApplicationUser
            {
                UserName = $"{prefix}{i}",
                Email = $"{prefix}{i}@load.test",
                FirstName = $"{prefix}FirstName{i}",
                LastName = $"{prefix}LastName{i}",
                IsEmailPublic = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user {user.UserName}: {errors}");
            }

            emails.Add(user.Email);
        }

        return emails;
    }
}
