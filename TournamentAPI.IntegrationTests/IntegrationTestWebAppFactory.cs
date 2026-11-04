using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Respawn;
using System.Threading.RateLimiting;
using Testcontainers.MsSql;
using TournamentAPI.Data;
using TournamentAPI.IntegrationTests.Infrastructure;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/azure-sql-edge:latest")
        .WithPassword("Your_password123")
        .Build();

    private Respawner _respawner = null!;

    private string ConnectionString => _dbContainer.GetConnectionString() + ";Initial Catalog=TournamentTestDb";

    public QueryCommandRecorder CommandRecorder { get; } = new();

    public BeforeCommandHook CommandHook { get; } = new();

    public RecordingStripeGateway StripeGateway { get; } = new();

    public RecordingStripeWebhookHandler RecordingWebhookHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_integration_placeholder",
                ["Stripe:WebhookSigningSecret"] = StripeWebhookPayloads.TestSigningSecret,
                ["Stripe:OnboardingRefreshUrl"] = "https://tournamentapi.test/payouts/onboarding/refresh",
                ["Stripe:OnboardingReturnUrl"] = "https://tournamentapi.test/payouts/onboarding/return"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextFactory<ApplicationDbContext>>();

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(ConnectionString);
                options.AddInterceptors(CommandRecorder, CommandHook);
            });

            services.AddHangfire(config => config.UseSqlServerStorage(ConnectionString));

            services.RemoveAll<IHostedService>();

            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetNoLimiter("no-limit"));
            });

            services.RemoveAll<IStripeGateway>();
            services.AddSingleton<IStripeGateway>(StripeGateway);

            services.AddSingleton<IStripeWebhookHandler>(RecordingWebhookHandler);
            services.AddSingleton<IStripeWebhookHandler, ThrowingStripeWebhookHandler>();
        });
    }

    public void ResetStripeFakes()
    {
        StripeGateway.Reset();
        RecordingWebhookHandler.Reset();
        CommandHook.Reset();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using (var context = new ApplicationDbContext(contextOptions))
        {
            await context.Database.EnsureCreatedAsync();
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            WithReseed = true
        });
    }

    public new Task DisposeAsync() => _dbContainer.StopAsync();

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}
