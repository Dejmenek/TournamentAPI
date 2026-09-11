using TournamentAPI.Data;
using TournamentAPI.EventListeners;

namespace TournamentAPI.Configuration.Extensions;

internal static class GraphQLExtensions
{
    internal static IServiceCollection AddApplicationGraphQL(this IServiceCollection services, bool isDevelopment)
    {
        services.AddHttpContextAccessor();
        services.AddTournamentApiDataLoaders();

        services
            .AddGraphQLServer()
            .AddTournamentApiTypes()
            .ModifyRequestOptions(options =>
            {
                options.ExecutionTimeout = TimeSpan.FromSeconds(30);
            })
            .DisableIntrospection(!isDevelopment)
            .AddHttpRequestInterceptor<HttpRequestInterceptor>()
            .AddApplicationService<ILogger<ExecutionEventListener>>()
            .AddDiagnosticEventListener<ExecutionEventListener>()
            .AddAuthorization()
            .RegisterDbContextFactory<ApplicationDbContext>()
            .AddMutationConventions()
            .AddQueryConventions()
            .AddPagingArguments()
            .AddProjections()
            .AddFiltering(x => x.AddDefaults())
            .AddSorting()
            .AddMaxExecutionDepthRule(10)
            .AddInstrumentation();

        return services;
    }
}
