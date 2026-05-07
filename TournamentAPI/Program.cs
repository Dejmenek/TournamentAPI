using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using TournamentAPI;
using TournamentAPI.Brackets;
using TournamentAPI.Configuration;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.EventListeners;
using TournamentAPI.Matches;
using TournamentAPI.Participants;
using TournamentAPI.Services;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;
using MatchType = TournamentAPI.Matches.MatchType;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<HealthCheckApiKeyOptions>()
    .BindConfiguration(HealthCheckApiKeyOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
{
    var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    options.UseSqlServer(dbOptions.DefaultConnection);
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(opt =>
{
    opt.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        {
            return RateLimitPartition.GetConcurrencyLimiter(
                "GlobalConcurrencyLimiter",
                _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 100,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }
            );
        })
    );
    options.AddPolicy("IpBasedTokenBucket", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress!.ToString();

        return RateLimitPartition.GetTokenBucketLimiter(
            clientIp,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                TokensPerPeriod = 50,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        );
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TournamentAPI"))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHotChocolateInstrumentation();
        tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddConsoleExporter();
    });

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((jwtBearerOptions, jwtOpts) =>
    {
        var jwt = jwtOpts.Value;
        jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = jwt.Audience,
            ValidIssuer = jwt.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });

builder.Services
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

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser().Build())
    .AddPolicy("HealthCheckPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            var httpContext = context.Resource as HttpContext;
            var apiKey = httpContext?.Request.Headers["X-Health-Check-Key"].ToString();
            var expectedKey = httpContext?.RequestServices
                .GetRequiredService<IOptions<HealthCheckApiKeyOptions>>()
                .Value.ApiKey;

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(expectedKey))
            {
                return false;
            }

            var apiKeySpan = Encoding.UTF8.GetBytes(apiKey).AsSpan();
            var expectedKeySpan = Encoding.UTF8.GetBytes(expectedKey).AsSpan();

            return CryptographicOperations.FixedTimeEquals(apiKeySpan, expectedKeySpan);
        }));

builder.Services
    .AddHttpContextAccessor()
    .AddGraphQLServer()
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

builder.Services.AddScoped<JwtService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();

    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(context, userManager);
}

app.UseRateLimiter();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
})
.RequireAuthorization("HealthCheckPolicy")
.RequireRateLimiting("IpBasedTokenBucket");

app.MapGraphQL()
    .RequireRateLimiting("IpBasedTokenBucket");

app.Run();

public partial class Program { }
