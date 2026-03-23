using NBomber.CSharp;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.LoadTests;

public class LatencyTests : BaseLoadTest, IClassFixture<LatencyWebAppFactory>
{
    public LatencyTests(LatencyWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void Api_ShouldMeetP95Latency_UnderNormalLoad()
    {
        // Arrange
        var client = CreateClient();
        var scenario = Scenario.Create("latency_baseline", async _ =>
        {
            var response = await client.ExecuteQueryAsync<TournamentsResponse>(
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches);

            return response.HasErrors ? Response.Fail() : Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(30)));

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var p95Latency = stats.ScenarioStats[0].Ok.Latency.Percent95;
        Assert.True(p95Latency < 500);
        Assert.Equal(0, stats.ScenarioStats[0].Fail.Request.Count);
    }

    [Fact]
    public void Api_ShouldDegradeGracefully_AsLoadIncreases()
    {
        // Arrange
        var client = CreateClient();
        var scenario = Scenario.Create("latency_under_load", async _ =>
        {
            var response = await client.ExecuteQueryAsync<TournamentsResponse>(
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches);
            return response.HasErrors ? Response.Fail() : Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 5, during: TimeSpan.FromSeconds(20)),
            Simulation.RampingConstant(copies: 15, during: TimeSpan.FromSeconds(20)),
            Simulation.RampingConstant(copies: 20, during: TimeSpan.FromSeconds(20))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        Assert.Equal(0, stats.ScenarioStats[0].Fail.Request.Count);
        Assert.True(stats.ScenarioStats[0].Ok.Latency.Percent95 < 800);
    }
}
