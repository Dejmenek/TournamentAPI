using TournamentAPI.Metrics;

namespace TournamentAPI.Configuration.Extensions;

internal static class MetricsExtensions
{
    internal static IServiceCollection AddApplicationMetrics(this IServiceCollection services)
    {
        services.AddSingleton<TournamentMetrics>();

        return services;
    }
}
