using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TournamentAPI.Data;

namespace TournamentAPI.Configuration.Extensions;

internal static class HealthChecksExtensions
{
    internal static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddSqlServer(
                connectionStringFactory: sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.DefaultConnection,
                name: "sqlserver",
                tags: ["database"],
                failureStatus: HealthStatus.Unhealthy
            )
            .AddDbContextCheck<ApplicationDbContext>(
                name: "database",
                tags: ["database"],
                failureStatus: HealthStatus.Unhealthy
            );

        return services;
    }
}
