using TournamentAPI.Metrics;

namespace TournamentAPI.Configuration.Extensions;

internal static class MetricsExtensions
{
    internal static IServiceCollection AddApplicationMetrics(this IServiceCollection services)
    {
        services.AddSingleton<TournamentMetrics>();
        services.AddSingleton<BracketMetrics>();
        services.AddSingleton<MatchMetrics>();
        services.AddSingleton<ParticipantMetrics>();
        services.AddSingleton<UserMetrics>();

        return services;
    }
}
