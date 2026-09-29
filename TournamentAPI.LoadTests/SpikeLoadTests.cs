using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using TournamentQueries = TournamentAPI.Shared.QueryExamples.Queries.Tournaments;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class SpikeLoadTests : BaseLoadTest, IClassFixture<MediumDatasetWebAppFactory>
{
    private const string WarmUp = "spike_warm_up";
    private const string PreSpike = "pre_spike";
    private const string Spike = "spike";
    private const string Drain = "spike_drain";
    private const string PostSpike = "post_spike";

    private static readonly LoadPhase[] Phases =
    [
        new(WarmUp, LoadTestBudgets.Spike.BaselineRatePerSecond, TimeSpan.FromSeconds(LoadTestBudgets.Spike.WarmUpSeconds)),
        new(PreSpike, LoadTestBudgets.Spike.BaselineRatePerSecond, TimeSpan.FromSeconds(LoadTestBudgets.Spike.BaselineSeconds)),
        new(Spike, LoadTestBudgets.Spike.BurstRatePerSecond, TimeSpan.FromSeconds(LoadTestBudgets.Spike.BurstSeconds)),
        new(Drain, LoadTestBudgets.Spike.BaselineRatePerSecond, TimeSpan.FromSeconds(LoadTestBudgets.Spike.DrainSeconds)),
        new(PostSpike, LoadTestBudgets.Spike.BaselineRatePerSecond, TimeSpan.FromSeconds(LoadTestBudgets.Spike.BaselineSeconds))
    ];

    public SpikeLoadTests(MediumDatasetWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void Api_ShouldAbsorbASpike_AndRecoverToBaselineLatency()
    {
        // Arrange
        using var client = CreateClient();
        var scenario = GraphQLLoadScenario.Create("browse_tournaments_spike", async context =>
            await GraphQLLoadStep.RunAsync(
                LoadPhases.NameAt(Phases, context.GetScenarioTimerTime()),
                context,
                client.HttpClient,
                TournamentQueries.GetAllWithBracketAndMatches,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithoutWarmUp()
        .WithLoadSimulations(LoadPhases.ToSimulations(Phases));

        var thresholds = new List<LoadThreshold>
        {
            LoadThresholds.ErrorBudget(0),
            LoadThresholds.StatusMix("200", 100)
        };

        foreach (var phase in new[] { PreSpike, PostSpike })
        {
            thresholds.Add(LoadThresholds.StepHealth(phase, 0));
            thresholds.Add(LoadThresholds.TailLatency(
                phase,
                LoadTestBudgets.Spike.BaselineP95Ms,
                LoadTestBudgets.Spike.BaselineP99Ms,
                LoadTestBudgets.Spike.BaselineMaxMs));
        }

        thresholds.Add(LoadThresholds.StepHealth(Spike, 0));
        thresholds.Add(LoadThresholds.TailLatency(
            Spike,
            LoadTestBudgets.Spike.BurstP95Ms,
            LoadTestBudgets.Spike.BurstP99Ms,
            LoadTestBudgets.Spike.BurstMaxMs));

        foreach (var phase in new[] { PreSpike, Spike, PostSpike })
        {
            thresholds.Add(LoadThresholds.PayloadGuard(
                phase,
                LoadTestBudgets.Spike.MinMeanPayloadBytes,
                LoadTestBudgets.Spike.MaxMeanPayloadBytes));
        }

        // Act
        var stats = LoadTestRun.Execute(scenario, thresholds.ToArray());

        // Assert
        var preSpikeP95 = P95(stats, PreSpike);
        var postSpikeP95 = P95(stats, PostSpike);
        var allowedP95 = Math.Max(
            preSpikeP95 * LoadTestBudgets.Spike.MaxRecoveryP95Ratio,
            LoadTestBudgets.Spike.RecoveryP95FloorMs);

        Assert.True(
            postSpikeP95 <= allowedP95,
            $"p95 after the spike ({postSpikeP95}ms) exceeds {LoadTestBudgets.Spike.MaxRecoveryP95Ratio}x the p95 before it ({preSpikeP95}ms) and the {LoadTestBudgets.Spike.RecoveryP95FloorMs}ms floor");
    }

    private static double P95(ScenarioStats stats, string stepName)
        => stats.StepStats.Get(stepName).Ok.Latency.Percent95;
}
