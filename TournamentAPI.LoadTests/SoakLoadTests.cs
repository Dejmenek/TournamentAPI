using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using Xunit.Abstractions;
using TournamentQueries = TournamentAPI.Shared.QueryExamples.Queries.Tournaments;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Soak")]
public class SoakLoadTests : BaseLoadTest, IClassFixture<MediumDatasetWebAppFactory>
{
    private const string ListScenario = "soak_browse_tournament_list";
    private const string ViewScenario = "soak_view_tournament_details";
    private const string DiscoverScenario = "soak_discover_tournaments";

    private static readonly TimeSpan WarmUp = TimeSpan.FromSeconds(LoadTestBudgets.Soak.WarmUpSeconds);
    private static readonly TimeSpan Segment = TimeSpan.FromSeconds(LoadTestBudgets.Soak.SegmentSeconds);
    private static readonly TimeSpan TotalDuration = WarmUp + Segment * LoadTestBudgets.Soak.SegmentCount;

    private readonly MediumDatasetWebAppFactory _factory;
    private readonly ITestOutputHelper _output;

    public SoakLoadTests(MediumDatasetWebAppFactory factory, ITestOutputHelper output) : base(factory)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public void Api_ShouldHoldItsLatencyAndMemory_UnderSustainedBrowsingTraffic()
    {
        // Arrange
        using var client = CreateClient();
        var http = client.HttpClient;
        var tournamentIds = _factory.TournamentIds;
        var searchTerm = _factory.TournamentSearchTerm;

        var browseList = GraphQLLoadScenario.Create(ListScenario, async context =>
            await GraphQLLoadStep.RunAsync(
                StepAt(StepNames.ListTournaments, context.GetScenarioTimerTime()),
                context,
                http,
                TournamentQueries.GetAllWithTotalCount,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithoutWarmUp()
        .WithLoadSimulations(Steady(LoadTestBudgets.Rates.ListTournamentsPerSecond));

        var viewDetails = GraphQLLoadScenario.Create(ViewScenario, async context =>
            await GraphQLLoadStep.RunAsync(
                StepAt(StepNames.ViewTournament, context.GetScenarioTimerTime()),
                context,
                http,
                TournamentQueries.GetByIdWithParticipants,
                new { id = tournamentIds[context.Random.Next(tournamentIds.Count)] },
                GraphQLLoadStep.NonNullField("tournamentById")))
        .WithoutWarmUp()
        .WithLoadSimulations(Steady(LoadTestBudgets.Rates.ViewTournamentPerSecond));

        var discover = GraphQLLoadScenario.Create(DiscoverScenario, async context =>
        {
            var search = await GraphQLLoadStep.RunAsync(
                StepAt(StepNames.SearchTournamentsByName, context.GetScenarioTimerTime()),
                context,
                http,
                TournamentQueries.GetAllWithNameFilter,
                new { nameFilter = searchTerm },
                GraphQLLoadStep.NonEmptyConnection("tournaments"));

            if (search.IsError)
            {
                return search;
            }

            return await GraphQLLoadStep.RunAsync(
                StepAt(StepNames.SortTournaments, context.GetScenarioTimerTime()),
                context,
                http,
                TournamentQueries.GetAllWithSorting,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments"));
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Steady(LoadTestBudgets.Rates.DiscoverTournamentsPerSecond));

        long? memoryAfterFirstSegment = null;
        using var memorySampler = new Timer(
            _ => memoryAfterFirstSegment = ManagedMemoryAfterCollection(),
            null,
            WarmUp + Segment,
            Timeout.InfiniteTimeSpan);

        // Act
        IReadOnlyDictionary<string, ScenarioStats> stats;
        long memoryAtEnd;

        try
        {
            stats = LoadTestRun.ExecuteAll(
                new ScenarioRun(browseList, ThresholdsFor(StepNames.ListTournaments)),
                new ScenarioRun(viewDetails, ThresholdsFor(StepNames.ViewTournament)),
                new ScenarioRun(discover, ThresholdsFor(StepNames.SearchTournamentsByName, StepNames.SortTournaments)));
        }
        finally
        {
            memoryAtEnd = ManagedMemoryAfterCollection();

            _output.WriteLine($"SOAK managed memory after collection: {(memoryAfterFirstSegment ?? 0) / 1024 / 1024} MB after the first segment, {memoryAtEnd / 1024 / 1024} MB at the end");
        }

        // Assert
        AssertNoLatencyDrift(stats[ListScenario], StepNames.ListTournaments);
        AssertNoLatencyDrift(stats[ViewScenario], StepNames.ViewTournament);
        AssertNoLatencyDrift(stats[DiscoverScenario], StepNames.SearchTournamentsByName);
        AssertNoLatencyDrift(stats[DiscoverScenario], StepNames.SortTournaments);

        Assert.NotNull(memoryAfterFirstSegment);

        var growth = memoryAtEnd - memoryAfterFirstSegment.Value;

        Assert.True(
            growth <= LoadTestBudgets.Soak.MaxManagedMemoryGrowthBytes,
            $"Managed memory after a full collection grew by {growth / 1024 / 1024} MB between the end of the first segment and the end of the run, above the {LoadTestBudgets.Soak.MaxManagedMemoryGrowthBytes / 1024 / 1024} MB limit");
    }

    private static string StepAt(string step, TimeSpan elapsed)
    {
        if (elapsed < WarmUp)
        {
            return $"{step}_warm_up";
        }

        var index = (int)((elapsed - WarmUp) / Segment) + 1;

        return SegmentStep(step, Math.Min(index, LoadTestBudgets.Soak.SegmentCount));
    }

    private static string SegmentStep(string step, int segment) => $"{step}_segment_{segment}";

    private static LoadSimulation Steady(int ratePerSecond)
        => Simulation.Inject(ratePerSecond, TimeSpan.FromSeconds(1), TotalDuration);

    private static LoadThreshold[] ThresholdsFor(params string[] steps)
    {
        var thresholds = new List<LoadThreshold>
        {
            LoadThresholds.ErrorBudget(0),
            LoadThresholds.StatusMix("200", 100)
        };

        foreach (var step in steps)
        {
            var budget = LoadTestBudgets.Steps.For(LoadTestDataSize.Medium, step);

            for (var segment = 1; segment <= LoadTestBudgets.Soak.SegmentCount; segment++)
            {
                thresholds.AddRange(LoadThresholds.ForBudget(budget with { Step = SegmentStep(step, segment) }));
            }
        }

        return thresholds.ToArray();
    }

    private static void AssertNoLatencyDrift(ScenarioStats stats, string step)
    {
        var firstP95 = stats.StepStats.Get(SegmentStep(step, 1)).Ok.Latency.Percent95;
        var lastP95 = stats.StepStats.Get(SegmentStep(step, LoadTestBudgets.Soak.SegmentCount)).Ok.Latency.Percent95;
        var allowedP95 = Math.Max(firstP95 * LoadTestBudgets.Soak.MaxDriftP95Ratio, LoadTestBudgets.Soak.DriftP95FloorMs);

        Assert.True(
            lastP95 <= allowedP95,
            $"p95 of {step} in the last segment ({lastP95}ms) exceeds {LoadTestBudgets.Soak.MaxDriftP95Ratio}x the first segment ({firstP95}ms) and the {LoadTestBudgets.Soak.DriftP95FloorMs}ms floor");
    }

    private static long ManagedMemoryAfterCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return GC.GetTotalMemory(forceFullCollection: true);
    }
}
