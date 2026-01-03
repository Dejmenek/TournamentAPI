using NBomber.CSharp;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.LoadTests;
public class LatencyTests : BaseLoadTest
{
    public LatencyTests(LoadTestWebAppFactory factory) : base(factory)
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
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.KeepConstant(copies: 30, during: TimeSpan.FromSeconds(30)));

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
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: 20, during: TimeSpan.FromSeconds(20)),
            Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(20)),
            Simulation.RampingConstant(copies: 100, during: TimeSpan.FromSeconds(20))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        Assert.True(
            stats.ScenarioStats[0].Ok.Latency.Percent95 < 1000
        );
    }
}
