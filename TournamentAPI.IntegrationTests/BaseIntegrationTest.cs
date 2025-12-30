using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Helpers;

namespace TournamentAPI.IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly IntegrationTestWebAppFactory Factory;
    private IServiceScope? _scope;
    protected ApplicationDbContext DbContext = null!;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
    }

    public virtual async Task InitializeAsync()
    {
        _scope = Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();

        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DatabaseSeeder.SeedAsync(DbContext, userManager);
    }

    public virtual Task DisposeAsync()
    {
        DbContext?.Dispose();
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    protected TestClient CreateClient()
    {
        var httpClient = Factory.CreateClient();
        return new TestClient(httpClient);
    }
}
