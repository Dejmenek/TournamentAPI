using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace TournamentAPI.LoadTests;

public class LatencyWebAppFactory : LoadTestWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetNoLimiter("no-limit"));

                options.AddPolicy<string>("IpBasedTokenBucket", _ =>
                    RateLimitPartition.GetNoLimiter("no-limit"));
            });
        });
    }
}
