using NBomber.CSharp;
using TournamentAPI.Configuration.Extensions;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class ConcurrencyLimiterTests : BaseLoadTest, IClassFixture<ConcurrencyLimiterWebAppFactory>
{
    public ConcurrencyLimiterTests(ConcurrencyLimiterWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void ConcurrencyLimiter_Should_RejectRequests_AboveThePermitLimit()
    {
        // Arrange
        using var client = CreateClient();

        var scenario = GraphQLLoadScenario.Create("concurrent_tournament_browsing", async context =>
            await GraphQLLoadStep.RunAsync(
                LoadTestBudgets.ListTournaments.Step,
                context,
                client.HttpClient,
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 400, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5))
        );

        // Act
        var stats = LoadTestRun.Execute(
            scenario,
            LoadThresholds.StatusMix("200", minOkPercent: 0, allowedFailCodes: "429"));

        // Assert
        Assert.True(stats.Ok.Request.Count >= RateLimiterExtensions.ConcurrencyPermitLimit);
        Assert.True(stats.Ok.Request.Count > RateLimiterExtensions.TokenBucketLimit);
        Assert.True(stats.Fail.Request.Count > 0);

        var rateLimitedRequests = Assert.Single(stats.Fail.StatusCodes);
        Assert.Equal("429", rateLimitedRequests.StatusCode);
        Assert.Equal(stats.Fail.Request.Count, rateLimitedRequests.Count);
    }
}
