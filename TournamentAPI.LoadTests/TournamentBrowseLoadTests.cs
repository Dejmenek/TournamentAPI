using NBomber.CSharp;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public abstract class TournamentBrowseLoadTestsBase : BaseLoadTest
{
    private static readonly TimeSpan WarmUp = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(30);

    private readonly LoadTestWebAppFactory _factory;

    protected TournamentBrowseLoadTestsBase(LoadTestWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    protected abstract LoadTestDataSize Size { get; }

    [Fact]
    public void Api_ShouldMeetBudgets_UnderMixedBrowsingTraffic()
    {
        // Arrange
        using var client = CreateClient();
        var http = client.HttpClient;
        var tournamentIds = _factory.TournamentIds;
        var searchTerm = _factory.TournamentSearchTerm;

        var browseList = GraphQLLoadScenario.Create("browse_tournament_list", async context =>
            await GraphQLLoadStep.RunAsync(
                StepNames.ListTournaments,
                context,
                http,
                Shared.QueryExamples.Queries.Tournaments.GetAllWithTotalCount,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments")))
        .WithWarmUpDuration(WarmUp)
        .WithLoadSimulations(Simulation.Inject(
            rate: LoadTestBudgets.Rates.ListTournamentsPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: Duration));

        var viewDetails = GraphQLLoadScenario.Create("view_tournament_details", async context =>
            await GraphQLLoadStep.RunAsync(
                StepNames.ViewTournament,
                context,
                http,
                Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipants,
                new { id = tournamentIds[context.Random.Next(tournamentIds.Count)] },
                GraphQLLoadStep.NonNullField("tournamentById")))
        .WithWarmUpDuration(WarmUp)
        .WithLoadSimulations(Simulation.Inject(
            rate: LoadTestBudgets.Rates.ViewTournamentPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: Duration));

        var discover = GraphQLLoadScenario.Create("discover_tournaments", async context =>
        {
            var search = await GraphQLLoadStep.RunAsync(
                StepNames.SearchTournamentsByName,
                context,
                http,
                Shared.QueryExamples.Queries.Tournaments.GetAllWithNameFilter,
                new { nameFilter = searchTerm },
                GraphQLLoadStep.NonEmptyConnection("tournaments"));

            if (search.IsError)
            {
                return search;
            }

            return await GraphQLLoadStep.RunAsync(
                StepNames.SortTournaments,
                context,
                http,
                Shared.QueryExamples.Queries.Tournaments.GetAllWithSorting,
                bodyCheck: GraphQLLoadStep.NonEmptyConnection("tournaments"));
        })
        .WithWarmUpDuration(WarmUp)
        .WithLoadSimulations(Simulation.Inject(
            rate: LoadTestBudgets.Rates.DiscoverTournamentsPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: Duration));

        // Act & Assert
        LoadTestRun.ExecuteAll(
            new ScenarioRun(browseList, LoadThresholds.ForSteps(Size, "200", StepNames.ListTournaments)),
            new ScenarioRun(viewDetails, LoadThresholds.ForSteps(Size, "200", StepNames.ViewTournament)),
            new ScenarioRun(discover, LoadThresholds.ForSteps(Size, "200", StepNames.SearchTournamentsByName, StepNames.SortTournaments)));
    }
}

public class DefaultDatasetTournamentBrowseLoadTests
    : TournamentBrowseLoadTestsBase, IClassFixture<LatencyWebAppFactory>
{
    public DefaultDatasetTournamentBrowseLoadTests(LatencyWebAppFactory factory) : base(factory)
    {
    }

    protected override LoadTestDataSize Size => LoadTestDataSize.Default;
}

public class MediumDatasetTournamentBrowseLoadTests
    : TournamentBrowseLoadTestsBase, IClassFixture<MediumDatasetWebAppFactory>
{
    public MediumDatasetTournamentBrowseLoadTests(MediumDatasetWebAppFactory factory) : base(factory)
    {
    }

    protected override LoadTestDataSize Size => LoadTestDataSize.Medium;
}

public class LargeDatasetTournamentBrowseLoadTests
    : TournamentBrowseLoadTestsBase, IClassFixture<LargeDatasetWebAppFactory>
{
    public LargeDatasetTournamentBrowseLoadTests(LargeDatasetWebAppFactory factory) : base(factory)
    {
    }

    protected override LoadTestDataSize Size => LoadTestDataSize.Large;
}
