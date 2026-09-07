using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using TournamentAPI.Data;

namespace TournamentAPI.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
        .WithPassword("Your_password123")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextFactory<ApplicationDbContext>>();

            var connectionString = _dbContainer.GetConnectionString() + ";Initial Catalog=TournamentTestDb";

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddHangfire(config => config.UseSqlServerStorage(connectionString));

            services.RemoveAll<IHostedService>();
        });
    }

    public Task InitializeAsync() => _dbContainer.StartAsync();

    public new Task DisposeAsync() => _dbContainer.StopAsync();
}
