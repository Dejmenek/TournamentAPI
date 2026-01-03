using NBomber.CSharp;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.LoadTests;

public class RateLimitingTests : BaseLoadTest
{
    public RateLimitingTests(LoadTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void TokenBucket_Should_AllowBurst_Then_Reject()
    {
        // Arrange
        var clientIp = new Bogus.DataSets.Internet().Ip();
        var client = CreateClient();
        client.HttpClient.DefaultRequestHeaders.Add("X-Forwarded-For", clientIp);

        var scenario = Scenario.Create("token_bucket_burst", async _ =>
        {
            var response = await client.ExecuteQueryAsync<TournamentsResponse>(
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches);

            if (response.HasErrors &&
                response.Errors?.Any(e => e.Extensions?.ContainsKey("statusCode") == true &&
                                         (int)e.Extensions["statusCode"] == 429) == true)
            {
                return Response.Fail(statusCode: "429", message: "Rate limited");
            }

            return response.HasErrors ? Response.Fail() : Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 300, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(2))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        Assert.InRange(stats.ScenarioStats[0].Ok.Request.Count, 90, 130);
        Assert.True(stats.ScenarioStats[0].Fail.Request.Count > 0);

        Assert.Single(stats.ScenarioStats[0].Fail.StatusCodes);
        var rateLimitedRequests = stats.ScenarioStats[0].Fail.StatusCodes.Single();

        Assert.Equal("429", rateLimitedRequests.StatusCode);
        Assert.Equal(stats.ScenarioStats[0].Fail.Request.Count, rateLimitedRequests.Count);
    }

    [Fact]
    public void ConcurrencyLimiter_Should_LimitConcurrentRequests()
    {
        // Arrange
        var client = CreateClient();

        var scenario = Scenario.Create("concurrency_limiter", async _ =>
        {
            var response = await client.ExecuteQueryAsync<TournamentsResponse>(
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches
            );

            await Task.Delay(1000);

            if (response.HasErrors &&
                response.Errors?.Any(e => e.Extensions?.ContainsKey("statusCode") == true &&
                                         (int)e.Extensions["statusCode"] == 429) == true)
            {
                return Response.Fail(statusCode: "429", message: "Rate limited");
            }

            return response.HasErrors ? Response.Fail() : Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 150, during: TimeSpan.FromSeconds(5))
        );

        // Act
        var stats = NBomberRunner.RegisterScenarios(scenario).Run();

        // Assert
        Assert.True(stats.ScenarioStats[0].Ok.Request.Count >= 100);
        Assert.True(stats.ScenarioStats[0].Fail.Request.Count > 0);

        Assert.Single(stats.ScenarioStats[0].Fail.StatusCodes);
        var rateLimitedRequests = stats.ScenarioStats[0].Fail.StatusCodes.Single();

        Assert.Equal("429", rateLimitedRequests.StatusCode);
        Assert.Equal(stats.ScenarioStats[0].Fail.Request.Count, rateLimitedRequests.Count);
    }
}
