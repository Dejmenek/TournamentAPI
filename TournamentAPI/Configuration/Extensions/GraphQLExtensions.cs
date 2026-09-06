using TournamentAPI.Brackets;
using TournamentAPI.Data;
using TournamentAPI.EventListeners;
using TournamentAPI.Matches;
using TournamentAPI.Participants;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;
using MatchType = TournamentAPI.Matches.MatchType;

namespace TournamentAPI.Configuration.Extensions;

internal static class GraphQLExtensions
{
    internal static IServiceCollection AddApplicationGraphQL(this IServiceCollection services, bool isDevelopment)
    {
        services.AddHttpContextAccessor();

        services
            .AddGraphQLServer()
            .DisableIntrospection(!isDevelopment)
            .AddHttpRequestInterceptor<HttpRequestInterceptor>()
            .AddDiagnosticEventListener<ExecutionEventListener>()
            .AddAuthorization()
            .RegisterDbContextFactory<ApplicationDbContext>()
            .AddQueryType<Query>()
            .AddTypeExtension<TournamentQueries>()
            .AddTypeExtension<UserQueries>()
            .AddTypeExtension<MatchQueries>()
            .AddMutationType<Mutation>()
            .AddTypeExtension<TournamentMutations>()
            .AddTypeExtension<UserMutations>()
            .AddTypeExtension<MatchMutations>()
            .AddTypeExtension<BracketMutations>()
            .AddTypeExtension<ParticipantMutations>()
            .AddMutationConventions()
            .AddQueryConventions()
            .AddType<TournamentType>()
            .AddType<BracketType>()
            .AddType<MatchType>()
            .AddType<TournamentParticipantType>()
            .AddType<ApplicationUserType>()
            .AddDataLoader<OwnerByTournamentIdDataLoader>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddMaxExecutionDepthRule(8)
            .AddInstrumentation();

        return services;
    }
}
