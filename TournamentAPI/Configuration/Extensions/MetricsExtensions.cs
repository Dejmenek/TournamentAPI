using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Metrics;

namespace TournamentAPI.Configuration.Extensions;

internal static class MetricsExtensions
{
    internal static IServiceCollection AddApplicationMetrics(this IServiceCollection services)
    {
        services.AddSingleton<TournamentMetrics>();
        services.AddHostedService(sp => sp.GetRequiredService<TournamentMetrics>());

        return services;
    }
}
