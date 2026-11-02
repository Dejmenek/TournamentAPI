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

        services
            .AddOptions<StripeOptions>()
            .BindConfiguration(StripeOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<PaidTournamentOptions>()
            .BindConfiguration(PaidTournamentOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
