using Microsoft.Extensions.DependencyInjection;

namespace TournamentAPI.Configuration.Extensions;

internal static class OptionsExtensions
{
    internal static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<HealthCheckApiKeyOptions>()
            .BindConfiguration(HealthCheckApiKeyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
