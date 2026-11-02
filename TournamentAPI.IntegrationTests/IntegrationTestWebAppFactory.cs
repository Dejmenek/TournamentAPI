using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Testcontainers.MsSql;
using TournamentAPI.Data;
using TournamentAPI.IntegrationTests.Infrastructure;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
        .WithPassword("Your_password123")
        .Build();

    public QueryCommandRecorder CommandRecorder { get; } = new();

    public RecordingStripeGateway StripeGateway { get; } = new();

    public RecordingStripeWebhookHandler RecordingWebhookHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_integration_placeholder",
                ["Stripe:WebhookSigningSecret"] = StripeWebhookPayloads.TestSigningSecret
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextFactory<ApplicationDbContext>>();

            var connectionString = _dbContainer.GetConnectionString() + ";Initial Catalog=TournamentTestDb";

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(CommandRecorder);
            });

            services.AddHangfire(config => config.UseSqlServerStorage(connectionString));

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
    }

    public Task InitializeAsync() => _dbContainer.StartAsync();

    public new Task DisposeAsync() => _dbContainer.StopAsync();
}
