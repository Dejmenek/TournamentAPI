using Hangfire;
using Microsoft.Extensions.Options;

namespace TournamentAPI.Configuration.Extensions;

internal static class HangfireExtensions
{
    internal static IServiceCollection AddApplicationHangfire(this IServiceCollection services)
    {
        services.AddHangfire((sp, config) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            config.UseSqlServerStorage(dbOptions.DefaultConnection);
        });

        services.AddHangfireServer();

        return services;
    }
}
