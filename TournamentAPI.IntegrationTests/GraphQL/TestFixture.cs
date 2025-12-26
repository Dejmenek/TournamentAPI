using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.IntegrationTests.GraphQL.Helpers;

namespace TournamentAPI.IntegrationTests.GraphQL;
public class TestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    public TestClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

                    services.AddDbContextFactory<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

        var httpClient = _factory.CreateClient();
        Client = new TestClient(httpClient);

        await Task.CompletedTask;
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.EnsureDeletedAsync();
        await DatabaseSeeder.SeedAsync(context, userManager, recreateDatabase: false);

        Client.ClearAuthToken();
    }

    public ApplicationDbContext CreateDbContext()
    {
        if (_factory == null)
            throw new InvalidOperationException("Factory not initialized");

        var scope = _factory.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        return dbContextFactory.CreateDbContext();
    }

    public async Task DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }
}
