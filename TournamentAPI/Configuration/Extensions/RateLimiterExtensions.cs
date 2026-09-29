using System.Threading.RateLimiting;

namespace TournamentAPI.Configuration.Extensions;

internal static class RateLimiterExtensions
{
    internal const int ConcurrencyPermitLimit = 100;
    internal const int TokenBucketLimit = 100;
    internal const int TokenBucketTokensPerPeriod = 50;
    internal static readonly TimeSpan TokenBucketReplenishmentPeriod = TimeSpan.FromMinutes(1);

    internal static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimiterExtensions).FullName!);

                var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                logger.LogWarning(
                    "Rate limit exceeded for {ClientIp} on {Path}",
                    clientIp,
                    context.HttpContext.Request.Path);

                return ValueTask.CompletedTask;
            };
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                CreateIpTokenBucketLimiter(),
                CreateConcurrencyLimiter());
        });

        return services;
    }

    internal static PartitionedRateLimiter<HttpContext> CreateIpTokenBucketLimiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetTokenBucketLimiter(
                clientIp,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = TokenBucketLimit,
                    TokensPerPeriod = TokenBucketTokensPerPeriod,
                    ReplenishmentPeriod = TokenBucketReplenishmentPeriod,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }
            );
        });
    }

    internal static PartitionedRateLimiter<HttpContext> CreateConcurrencyLimiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetConcurrencyLimiter(
                "GlobalConcurrencyLimiter",
                _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = ConcurrencyPermitLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }
            )
        );
    }
}
