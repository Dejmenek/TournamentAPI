using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Brackets;
using TournamentAPI.Configuration.Extensions;
using TournamentAPI.Matches;
using TournamentAPI.Participants;
using TournamentAPI.Services;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;

namespace TournamentAPI.UnitTests.Schema;

public class ApplicationUserSchemaTests
{
    private static async Task<ISchemaDefinition> BuildSchemaAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-unit-test-signing-key",
                ["Jwt:Issuer"] = "unit-test-issuer",
                ["Jwt:Audience"] = "unit-test-audience",
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=schema-test;TrustServerCertificate=True;",
                ["HealthCheck:ApiKey"] = "unit-test-health-check-key"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddMetrics();
        services.AddScoped<MatchService>();
        services.AddScoped<ApplicationUserService>();
        services.AddScoped<BracketLookupService>();
        services.AddScoped<BracketService>();
        services.AddScoped<BracketCompletionService>();
        services.AddScoped<MatchCascadePositionCalculator>();
        services.AddScoped<MatchCorrectionService>();
        services.AddScoped<ParticipantsService>();
        services.AddScoped<TournamentLookupService>();
        services.AddScoped<UserTournamentsService>();
        services.AddScoped<JwtService>();
        services.AddApplicationOptions();
        services.AddApplicationDatabase();
        services.AddApplicationAuthentication();
        services.AddApplicationAuthorization();
        services.AddApplicationMetrics();
        services.AddApplicationGraphQL(isDevelopment: true);

        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IRequestExecutorManager>();
        var executor = await manager.GetExecutorAsync();

        return executor.Schema;
    }

    [Fact]
    public async Task ApplicationUserType_ExposesExactlyAllowListedFields()
    {
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<IObjectTypeDefinition>("ApplicationUser");

        var fieldNames = type.Fields
            .Where(f => !f.IsIntrospectionField)
            .Select(f => f.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            "email", "firstName", "id", "isEmailPublic", "lastName",
            "playedTournaments", "wonMatches", "wonTournaments"
        }
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, fieldNames);
    }

    [Fact]
    public async Task ApplicationUserType_DoesNotExposeIdentityInternalFields()
    {
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<IObjectTypeDefinition>("ApplicationUser");

        var fieldNames = type.Fields.Select(f => f.Name).ToHashSet();

        var identityInternalFields = new[] { "userName", "passwordHash", "securityStamp", "phoneNumber" };

        foreach (var field in identityInternalFields)
        {
            Assert.DoesNotContain(field, fieldNames);
        }
    }
}
