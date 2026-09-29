using NBomber.CSharp;
using TournamentAPI.Configuration.Extensions;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class TokenBucketTests : BaseLoadTest, IClassFixture<TokenBucketWebAppFactory>
{
    public TokenBucketTests(TokenBucketWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void TokenBucket_Should_AllowBurst_Then_Reject()
    {
        // Arrange
        using var client = CreateClient();

        var scenario = GraphQLLoadScenario.Create("tournament_browsing_burst", async context =>
            await GraphQLLoadStep.RunAsync(
                LoadTestBudgets.ListTournaments.Step,
                context,
                client.HttpClient,
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 300, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(2))
        );

        // Act
        var stats = LoadTestRun.Execute(
            scenario,
            LoadThresholds.StatusMix("200", minOkPercent: 0, allowedFailCodes: "429"));

        // Assert
        Assert.InRange(
            stats.Ok.Request.Count,
            RateLimiterExtensions.TokenBucketLimit - 10,
            RateLimiterExtensions.TokenBucketLimit + 30);
        Assert.True(stats.Fail.Request.Count > 0);

        var rateLimitedRequests = Assert.Single(stats.Fail.StatusCodes);
        Assert.Equal("429", rateLimitedRequests.StatusCode);
        Assert.Equal(stats.Fail.Request.Count, rateLimitedRequests.Count);
    }
}
