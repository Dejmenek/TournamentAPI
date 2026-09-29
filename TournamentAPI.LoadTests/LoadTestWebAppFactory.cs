using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using TournamentAPI.Data;

namespace TournamentAPI.LoadTests;

public class LoadTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ApiKey = "pk_load_test_9f3a7c1e5b2d4086";

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
        .WithPassword("Your_password123")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey:Value"] = ApiKey
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IDbContextFactory<ApplicationDbContext>));

            var connectionString = _dbContainer.GetConnectionString() + ";Initial Catalog=TournamentLoadTestDb";

            services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

            services.AddHangfire(config => config.UseSqlServerStorage(connectionString));

            services.RemoveAll<IHostedService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<TournamentAPI.Data.Models.ApplicationUser>>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(context, userManager);
    }

    public new Task DisposeAsync() => _dbContainer.StopAsync();
}