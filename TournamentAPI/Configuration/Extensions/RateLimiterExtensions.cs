using System.Threading.RateLimiting;

namespace TournamentAPI.Configuration.Extensions;

internal static class RateLimiterExtensions
{
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
                PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetConcurrencyLimiter(
                        "GlobalConcurrencyLimiter",
                        _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = 100,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }
                    )
                )
            );
            options.AddPolicy("IpBasedTokenBucket", httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetTokenBucketLimiter(
                    clientIp,
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 100,
                        TokensPerPeriod = 50,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }
                );
            });
        });

        return services;
    }
}
