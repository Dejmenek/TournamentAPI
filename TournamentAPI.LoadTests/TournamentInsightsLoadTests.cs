using System.Text.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using TournamentQueries = TournamentAPI.Shared.QueryExamples.Queries.Tournaments;
using UserQueries = TournamentAPI.Shared.QueryExamples.Queries.Users;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public abstract class TournamentInsightsLoadTestsBase : BaseLoadTest
{
    private const string PageLimitRejectedCode = "PAGE_SIZE_LIMIT_EXCEEDED";
    private const int LoggedInUsers = 10;

    private static readonly TimeSpan WarmUp = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(30);

    private readonly LoadTestWebAppFactory _factory;

    protected TournamentInsightsLoadTestsBase(LoadTestWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    protected abstract LoadTestDataSize Size { get; }

    [Fact]
    public void Api_ShouldMeetBudgets_ForExpensiveQueryShapes()
    {
        // Arrange
        using var client = CreateClient();
        var http = client.HttpClient;
        var tournamentIds = _factory.TournamentIds;
        var roundRobinIds = _factory.RoundRobinTournamentIds;

        var ownerHistory = InspectTournament(
            "inspect_owner_history", StepNames.ViewOwnerHistory, http, TournamentQueries.GetByIdWithOwnerTournamentHistory, tournamentIds);

        var participantsWonTournaments = InspectTournament(
            "inspect_participants_won_tournaments", StepNames.ViewParticipantsWonTournaments, http, TournamentQueries.GetByIdWithParticipantsWonTournaments, tournamentIds);

        var participantsPlayedTournaments = InspectTournament(
            "inspect_participants_played_tournaments", StepNames.ViewParticipantsPlayedTournaments, http, TournamentQueries.GetByIdWithParticipantsPlayedTournaments, tournamentIds);

        var participantsWonMatches = InspectTournament(
            "inspect_participants_won_matches", StepNames.ViewParticipantsWonMatches, http, TournamentQueries.GetByIdWithParticipantsWonMatches, tournamentIds);

        var bracketStandings = InspectTournament(
            "inspect_bracket_standings", StepNames.ViewBracketStandings, http, TournamentQueries.GetByIdWithBracketStandings, roundRobinIds, HasStandings);

        VirtualUserSession[] sessions = [];

        var ownHistory = GraphQLLoadScenario.Create("inspect_own_tournament_history", async context =>
        {
            var session = VirtualUserSessions.Pick(sessions, context.InvocationNumber);

            return await GraphQLLoadStep.RunAsync(
                StepNames.ViewOwnTournamentHistory,
                context,
                session.Client,
                UserQueries.GetMeWithTournamentHistory,
                bodyCheck: GraphQLLoadStep.NonNullField("me"),
                bearerToken: session.AccessToken);
        })
        .WithInit(async _ =>
        {
            sessions = await VirtualUserSessions.LoginAsync(CreateClient, _factory.UserEmails, _factory.UserPassword, LoggedInUsers);
        })
        .WithWarmUpDuration(WarmUp)
        .WithLoadSimulations(Steady());

        var oversizedPage = GraphQLLoadScenario.Create("request_oversized_page", async context =>
            await GraphQLLoadStep.RunAsync(
                StepNames.ListTournamentsWithOversizedPage,
                context,
                http,
                TournamentQueries.GetAllWithExcessivePageSize,
                classifyRejection: GraphQLLoadStep.RejectionWhenErrorHasExtension("maxAllowedItems", PageLimitRejectedCode)))
        .WithWarmUpDuration(WarmUp)
        .WithLoadSimulations(Steady());

        // Act & Assert
        LoadTestRun.ExecuteAll(
            new ScenarioRun(ownerHistory, LoadThresholds.ForSteps(Size, "200", StepNames.ViewOwnerHistory)),
            new ScenarioRun(participantsWonTournaments, LoadThresholds.ForSteps(Size, "200", StepNames.ViewParticipantsWonTournaments)),
            new ScenarioRun(participantsPlayedTournaments, LoadThresholds.ForSteps(Size, "200", StepNames.ViewParticipantsPlayedTournaments)),
            new ScenarioRun(participantsWonMatches, LoadThresholds.ForSteps(Size, "200", StepNames.ViewParticipantsWonMatches)),
            new ScenarioRun(bracketStandings, LoadThresholds.ForSteps(Size, "200", StepNames.ViewBracketStandings)),
            new ScenarioRun(ownHistory, LoadThresholds.ForSteps(Size, "200", StepNames.ViewOwnTournamentHistory)),
            new ScenarioRun(oversizedPage, LoadThresholds.ForSteps(Size, PageLimitRejectedCode, StepNames.ListTournamentsWithOversizedPage)));
    }

    private static ScenarioProps InspectTournament(
        string scenarioName,
        string stepName,
        HttpClient http,
        string query,
        IReadOnlyList<int> tournamentIds,
        Func<JsonElement, bool>? bodyCheck = null)
    {
        return GraphQLLoadScenario.Create(scenarioName, async context =>
            await GraphQLLoadStep.RunAsync(
                stepName,
                context,
                http,
                query,
                new { id = tournamentIds[context.Random.Next(tournamentIds.Count)] },
                bodyCheck ?? GraphQLLoadStep.NonNullField("tournamentById")))
        .WithWarmUpDuration(WarmUp)
        .WithLoadSimulations(Steady());
    }

    private static LoadSimulation Steady()
    {
        return Simulation.Inject(
            rate: LoadTestBudgets.Rates.InsightPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: Duration);
    }

    private static bool HasStandings(JsonElement data)
    {
        return data.TryGetProperty("tournamentById", out var tournament)
            && tournament.TryGetProperty("bracket", out var bracket)
            && bracket.TryGetProperty("standings", out var standings)
            && standings.ValueKind == JsonValueKind.Array
            && standings.GetArrayLength() > 0;
    }
}

public class MediumDatasetTournamentInsightsLoadTests
    : TournamentInsightsLoadTestsBase, IClassFixture<MediumDatasetWebAppFactory>
{
    public MediumDatasetTournamentInsightsLoadTests(MediumDatasetWebAppFactory factory) : base(factory)
    {
    }

    protected override LoadTestDataSize Size => LoadTestDataSize.Medium;
}

public class LargeDatasetTournamentInsightsLoadTests
    : TournamentInsightsLoadTestsBase, IClassFixture<LargeDatasetWebAppFactory>
{
    public LargeDatasetTournamentInsightsLoadTests(LargeDatasetWebAppFactory factory) : base(factory)
    {
    }

    protected override LoadTestDataSize Size => LoadTestDataSize.Large;
}
