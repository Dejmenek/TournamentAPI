using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using TournamentQueries = TournamentAPI.Shared.QueryExamples.Queries.Tournaments;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Soak")]
public class BreakpointLoadTests : BaseLoadTest, IClassFixture<MediumDatasetWebAppFactory>
{
    private static readonly LoadPhase WarmUp = new(
        "breakpoint_warm_up",
        LoadTestBudgets.Breakpoint.RatesPerSecond[0],
        TimeSpan.FromSeconds(LoadTestBudgets.Breakpoint.WarmUpSeconds));

    private static readonly LoadPhase[] RatePhases = LoadTestBudgets.Breakpoint.RatesPerSecond
        .Select(rate => new LoadPhase(
            PhaseName(rate),
            rate,
            TimeSpan.FromSeconds(LoadTestBudgets.Breakpoint.PhaseSeconds)))
        .ToArray();

    private static readonly LoadPhase[] Phases = [WarmUp, .. RatePhases];

    public BreakpointLoadTests(MediumDatasetWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public void Api_ShouldSustainAtLeastTheDocumentedRate_BeforeLatencyBreaks()
    {
        // Arrange
        using var client = CreateClient();
        var scenario = GraphQLLoadScenario.Create("browse_tournaments_breakpoint", async context =>
            await GraphQLLoadStep.RunAsync(
                LoadPhases.NameAt(Phases, context.GetScenarioTimerTime()),
                context,
                client.HttpClient,
                TournamentQueries.GetAllWithBracketAndMatches,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithoutWarmUp()
        .WithLoadSimulations(LoadPhases.ToSimulations(Phases))
        .WithThresholds(
            LoadThresholds.AbortOnErrors(
                LoadTestBudgets.Breakpoint.SafetyMaxFailPercent,
                LoadTestBudgets.Breakpoint.SafetyAbortWhenErrorCount).ToNBomberThreshold());

        // Act
        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var stats = result.ScenarioStats.Get(scenario.ScenarioName);
        var breakpoint = FindBreakpoint(stats);

        Assert.True(
            breakpoint >= LoadTestBudgets.Breakpoint.MinBreakpointRatePerSecond,
            $"The breakpoint is {breakpoint} req/s, below the documented floor of {LoadTestBudgets.Breakpoint.MinBreakpointRatePerSecond} req/s. p95 per rate: {DescribeRates(stats)}");
    }

    private static string PhaseName(int rate) => $"breakpoint_rate_{rate}";

    private static int FindBreakpoint(ScenarioStats stats)
    {
        var breakpoint = 0;

        foreach (var phase in RatePhases)
        {
            if (!stats.StepStats.Exists(phase.Name))
            {
                break;
            }

            var step = stats.StepStats.Get(phase.Name);
            var isHealthy = step.Fail.Request.Count == 0
                && step.Ok.Latency.Percent95 <= LoadTestBudgets.Breakpoint.CeilingP95Ms;

            if (!isHealthy)
            {
                break;
            }

            breakpoint = phase.RatePerSecond;
        }

        return breakpoint;
    }

    private static string DescribeRates(ScenarioStats stats)
    {
        return string.Join(", ", RatePhases.Select(phase => stats.StepStats.Exists(phase.Name)
            ? $"{phase.RatePerSecond} req/s = {stats.StepStats.Get(phase.Name).Ok.Latency.Percent95}ms"
            : $"{phase.RatePerSecond} req/s = not reached"));
    }
}
