using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using TournamentAPI.Configuration.Extensions;
using TournamentAPI.Payments;

namespace TournamentAPI.UnitTests.Configuration;

public class RateLimiterExtensionsTests
{
    private static readonly IPAddress ClientIp = IPAddress.Parse("203.0.113.10");

    [Fact]
    public void StripeWebhookRequests_AreNotCountedByTheIpTokenBucket()
    {
        // Arrange
        using var limiter = RateLimiterExtensions.CreateGlobalLimiter();
        var webhookResults = new List<bool>();
        var graphQLLeases = new List<RateLimitLease>();

        // Act
        for (var i = 0; i < RateLimiterExtensions.TokenBucketLimit + 50; i++)
        {
            using var webhookLease = limiter.AttemptAcquire(CreateContext(StripeWebhookEndpoint.Path));
            webhookResults.Add(webhookLease.IsAcquired);
        }

        for (var i = 0; i < RateLimiterExtensions.TokenBucketLimit; i++)
            graphQLLeases.Add(limiter.AttemptAcquire(CreateContext("/graphql")));

        // Assert
        Assert.All(webhookResults, Assert.True);
        Assert.All(graphQLLeases, lease => Assert.True(lease.IsAcquired));
        graphQLLeases.ForEach(lease => lease.Dispose());
    }

    [Fact]
    public void OtherRequests_StillDrainTheIpTokenBucket()
    {
        // Arrange
        using var limiter = RateLimiterExtensions.CreateGlobalLimiter();

        // Act
        for (var i = 0; i < RateLimiterExtensions.TokenBucketLimit; i++)
            limiter.AttemptAcquire(CreateContext("/graphql")).Dispose();

        using var rejectedLease = limiter.AttemptAcquire(CreateContext("/graphql"));

        // Assert
        Assert.False(rejectedLease.IsAcquired);
    }

    [Fact]
    public void StripeWebhookRequests_AreCountedByTheConcurrencyLimiter()
    {
        // Arrange
        using var limiter = RateLimiterExtensions.CreateGlobalLimiter();
        var heldWebhookLeases = Enumerable.Range(0, RateLimiterExtensions.ConcurrencyPermitLimit)
            .Select(_ => limiter.AttemptAcquire(CreateContext(StripeWebhookEndpoint.Path)))
            .ToList();

        // Act
        using var rejectedLease = limiter.AttemptAcquire(CreateContext(StripeWebhookEndpoint.Path));

        // Assert
        Assert.All(heldWebhookLeases, lease => Assert.True(lease.IsAcquired));
        Assert.False(rejectedLease.IsAcquired);
        heldWebhookLeases.ForEach(lease => lease.Dispose());
    }

    private static HttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = ClientIp;
        return context;
    }
}
