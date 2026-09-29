using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

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

    public IReadOnlyList<int> TournamentIds { get; private set; } = [];

    public IReadOnlyList<int> RoundRobinTournamentIds { get; private set; } = [];

    public IReadOnlyList<string> UserEmails { get; private set; } = [];

    public virtual string UserPassword => "Password123!";

    public virtual string TournamentSearchTerm => "Cup";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedAsync(context, userManager);

        TournamentIds = await context.Tournaments
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => t.Id)
            .ToListAsync();

        RoundRobinTournamentIds = await context.Tournaments
            .AsNoTracking()
            .Where(t => t.Format == TournamentFormat.RoundRobin && t.Bracket != null)
            .OrderBy(t => t.Id)
            .Select(t => t.Id)
            .ToListAsync();

        UserEmails = await context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => u.Email!)
            .ToListAsync();
    }

    protected virtual Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        => DatabaseSeeder.SeedAsync(context, userManager);

    public new Task DisposeAsync() => _dbContainer.StopAsync();
}