using NBomber.Contracts;
using NBomber.CSharp;
using ListBudget = TournamentAPI.LoadTests.LoadTestBudgets.ListTournaments;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class LatencyTests : BaseLoadTest, IClassFixture<LatencyWebAppFactory>
{
    private const string SteadyBaseline = "steady_baseline";
    private const string SteadyPeak = "steady_peak";
    private const string RecoveredBaseline = "recovered_baseline";

    private sealed record RampPhase(string Name, int RatePerSecond, TimeSpan Duration, bool IsRamp);

    private static readonly RampPhase[] RampPhases =
    [
        new("ramp_to_baseline", LoadTestBudgets.Ramp.BaselineRatePerSecond, TimeSpan.FromSeconds(10), true),
        new(SteadyBaseline, LoadTestBudgets.Ramp.BaselineRatePerSecond, TimeSpan.FromSeconds(15), false),
        new("ramp_to_mid", LoadTestBudgets.Ramp.MidRatePerSecond, TimeSpan.FromSeconds(10), true),
        new("steady_mid", LoadTestBudgets.Ramp.MidRatePerSecond, TimeSpan.FromSeconds(15), false),
        new("ramp_to_peak", LoadTestBudgets.Ramp.PeakRatePerSecond, TimeSpan.FromSeconds(10), true),
        new(SteadyPeak, LoadTestBudgets.Ramp.PeakRatePerSecond, TimeSpan.FromSeconds(15), false),
        new("ramp_down_to_baseline", LoadTestBudgets.Ramp.BaselineRatePerSecond, TimeSpan.FromSeconds(10), true),
        new(RecoveredBaseline, LoadTestBudgets.Ramp.BaselineRatePerSecond, TimeSpan.FromSeconds(15), false)
    ];

    public LatencyTests(LatencyWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void Api_ShouldMeetLatencyBudget_UnderNormalLoad()
    {
        // Arrange
        using var client = CreateClient();
        var scenario = GraphQLLoadScenario.Create("browse_tournaments", async context =>
            await GraphQLLoadStep.RunAsync(
                ListBudget.Step,
                context,
                client.HttpClient,
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(Simulation.Inject(
            rate: LoadTestBudgets.Load.NormalRatePerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(30)));

        // Act & Assert
        LoadTestRun.Execute(
            scenario,
            LoadThresholds.ErrorBudget(ListBudget.MaxErrorPercent),
            LoadThresholds.StatusMix("200", ListBudget.MinOkStatusPercent),
            LoadThresholds.StepHealth(ListBudget.Step, ListBudget.MaxErrorPercent),
            LoadThresholds.TailLatency(ListBudget.Step, ListBudget.P95Ms, ListBudget.P99Ms, ListBudget.MaxMs),
            LoadThresholds.PayloadGuard(ListBudget.Step, ListBudget.MinMeanPayloadBytes, ListBudget.MaxMeanPayloadBytes));
    }

    [Fact]
    public void Api_ShouldDegradeGracefully_AsLoadIncreases_AndRecover_AsItDrops()
    {
        // Arrange
        using var client = CreateClient();
        var scenario = GraphQLLoadScenario.Create("browse_tournaments_ramp", async context =>
            await GraphQLLoadStep.RunAsync(
                PhaseAt(context.GetScenarioTimerTime()),
                context,
                client.HttpClient,
                Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithoutWarmUp()
        .WithLoadSimulations(RampPhases
            .Select(phase => phase.IsRamp
                ? Simulation.RampingInject(phase.RatePerSecond, TimeSpan.FromSeconds(1), phase.Duration)
                : Simulation.Inject(phase.RatePerSecond, TimeSpan.FromSeconds(1), phase.Duration))
            .ToArray());

        var thresholds = new List<LoadThreshold>
        {
            LoadThresholds.ErrorBudget(ListBudget.MaxErrorPercent),
            LoadThresholds.StatusMix("200", ListBudget.MinOkStatusPercent)
        };

        foreach (var phase in RampPhases.Where(p => !p.IsRamp))
        {
            thresholds.Add(LoadThresholds.StepHealth(phase.Name, ListBudget.MaxErrorPercent));
            thresholds.Add(LoadThresholds.TailLatency(phase.Name, LoadTestBudgets.Ramp.P95PerLevelMs, LoadTestBudgets.Ramp.P99Ms, LoadTestBudgets.Ramp.MaxMs));
            thresholds.Add(LoadThresholds.PayloadGuard(phase.Name, ListBudget.MinMeanPayloadBytes, ListBudget.MaxMeanPayloadBytes));
        }

        // Act
        var stats = LoadTestRun.Execute(scenario, thresholds.ToArray());

        // Assert
        var baselineP95 = P95(stats, SteadyBaseline);
        var peakP95 = P95(stats, SteadyPeak);
        var recoveredP95 = P95(stats, RecoveredBaseline);

        Assert.True(
            peakP95 <= baselineP95 * LoadTestBudgets.Ramp.MaxTopToBottomP95Ratio,
            $"p95 at {LoadTestBudgets.Ramp.PeakRatePerSecond} req/s ({peakP95}ms) exceeds {LoadTestBudgets.Ramp.MaxTopToBottomP95Ratio}x the p95 at {LoadTestBudgets.Ramp.BaselineRatePerSecond} req/s ({baselineP95}ms)");
        Assert.True(
            recoveredP95 <= baselineP95 * LoadTestBudgets.Ramp.MaxRecoveryP95Ratio,
            $"p95 after dropping back to {LoadTestBudgets.Ramp.BaselineRatePerSecond} req/s ({recoveredP95}ms) exceeds {LoadTestBudgets.Ramp.MaxRecoveryP95Ratio}x the initial p95 ({baselineP95}ms)");
    }

    private static string PhaseAt(TimeSpan elapsed)
    {
        var phaseEnd = TimeSpan.Zero;

        foreach (var phase in RampPhases)
        {
            phaseEnd += phase.Duration;
            if (elapsed < phaseEnd)
            {
                return phase.Name;
            }
        }

        return RampPhases[^1].Name;
    }

    private static double P95(NBomber.Contracts.Stats.ScenarioStats stats, string stepName)
        => stats.StepStats.Get(stepName).Ok.Latency.Percent95;
}
