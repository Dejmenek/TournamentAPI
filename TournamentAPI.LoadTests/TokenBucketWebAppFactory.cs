using Microsoft.AspNetCore.RateLimiting;

namespace TournamentAPI.LoadTests;

public class TokenBucketWebAppFactory : RateLimiterOverrideWebAppFactory
{
    protected override void ConfigureRateLimiter(RateLimiterOptions options)
    {
        UseOnlyIpTokenBucketLimiter(options);
    }
}
