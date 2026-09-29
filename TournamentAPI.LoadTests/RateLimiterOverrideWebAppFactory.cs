using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using TournamentAPI.Configuration.Extensions;

namespace TournamentAPI.LoadTests;

public abstract class RateLimiterOverrideWebAppFactory : LoadTestWebAppFactory
{
    protected abstract void ConfigureRateLimiter(RateLimiterOptions options);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                ConfigureRateLimiter(options);
            });
        });
    }

    protected static void DisableLimiters(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetNoLimiter("no-limit"));
    }

    protected static void UseOnlyConcurrencyLimiter(RateLimiterOptions options)
    {
        options.GlobalLimiter = RateLimiterExtensions.CreateConcurrencyLimiter();
    }

    protected static void UseOnlyIpTokenBucketLimiter(RateLimiterOptions options)
    {
        options.GlobalLimiter = RateLimiterExtensions.CreateIpTokenBucketLimiter();
    }
}
