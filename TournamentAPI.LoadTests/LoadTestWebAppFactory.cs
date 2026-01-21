using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using TournamentAPI.Data;

namespace TournamentAPI.LoadTests;

public class LoadTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
        .WithPassword("Your_password123")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IDbContextFactory<ApplicationDbContext>));

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                var connectionString = _dbContainer.GetConnectionString() + ";Initial Catalog=TournamentLoadTestDb";
                options.UseSqlServer(connectionString);
            });
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