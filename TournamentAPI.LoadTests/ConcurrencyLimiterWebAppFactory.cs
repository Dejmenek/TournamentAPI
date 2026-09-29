using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using TournamentAPI.Data;

namespace TournamentAPI.LoadTests;

public class ConcurrencyLimiterWebAppFactory : RateLimiterOverrideWebAppFactory
{
    public static readonly TimeSpan DbContextCreationDelay = TimeSpan.FromMilliseconds(300);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.DelayDbContextCreation<ApplicationDbContext>(DbContextCreationDelay));
    }

    protected override void ConfigureRateLimiter(RateLimiterOptions options)
    {
        UseOnlyConcurrencyLimiter(options);
    }
}
