using Microsoft.AspNetCore.Identity;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.LoadTests;

public abstract class DatasetWebAppFactory : LatencyWebAppFactory
{
    protected abstract LoadTestDataSize Size { get; }

    public override string UserPassword => LoadTestDataSeeder.UserPassword;

    public override string TournamentSearchTerm => LoadTestDataSeeder.TournamentNamePrefix;

    protected override Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        => LoadTestDataSeeder.SeedAsync(context, userManager, Size);
}

public class MediumDatasetWebAppFactory : DatasetWebAppFactory
{
    protected override LoadTestDataSize Size => LoadTestDataSize.Medium;
}

public class LargeDatasetWebAppFactory : DatasetWebAppFactory
{
    protected override LoadTestDataSize Size => LoadTestDataSize.Large;
}
