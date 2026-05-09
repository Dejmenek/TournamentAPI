using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Configuration.Extensions;

internal static class DatabaseExtensions
{
    internal static IServiceCollection AddApplicationDatabase(this IServiceCollection services)
    {
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(dbOptions.DefaultConnection);
        });

        services.AddIdentity<ApplicationUser, IdentityRole<int>>(opt =>
        {
            opt.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();

        return services;
    }
}
