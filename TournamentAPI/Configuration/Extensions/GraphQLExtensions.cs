using TournamentAPI.Data;
using TournamentAPI.EventListeners;
using TournamentAPI.Metrics;

namespace TournamentAPI.Configuration.Extensions;

internal static class GraphQLExtensions
{
    internal static IServiceCollection AddApplicationGraphQL(this IServiceCollection services, bool isDevelopment)
    {
        services.AddHttpContextAccessor();
        services.AddTournamentApiDataLoaders();

        services
            .AddGraphQLServer()
            .AddHttpResponseFormatter<CustomHttpResponseFormatter>()
            .AddTournamentApiTypes()
            .ModifyRequestOptions(options =>
            {
                options.ExecutionTimeout = TimeSpan.FromSeconds(30);
            })
            .DisableIntrospection(!isDevelopment)
            .AddApplicationService<ILogger<HttpRequestInterceptor>>()
            .AddHttpRequestInterceptor<HttpRequestInterceptor>()
            .AddApplicationService<ILogger<ExecutionEventListener>>()
            .AddApplicationService<GraphQLMetrics>()
            .AddDiagnosticEventListener<ExecutionEventListener>()
            .AddApplicationService<ILogger<UnhandledExceptionErrorFilter>>()
            .AddErrorFilter<UnhandledExceptionErrorFilter>()
            .AddAuthorization()
            .RegisterDbContextFactory<ApplicationDbContext>()
            .AddMutationConventions()
            .AddQueryConventions()
            .AddPagingArguments()
            .AddFiltering(x => x.AddDefaults().MaxAllowedFilterOperations(32))
            .AddSorting()
            .AddMaxExecutionDepthRule(10)
            .ModifyCostOptions(options =>
            {
                options.MaxFieldCost = 3_000;
            })
            .AddInstrumentation();

        return services;
    }
}
