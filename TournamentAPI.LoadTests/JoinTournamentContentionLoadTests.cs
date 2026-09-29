using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NBomber.CSharp;
using TournamentAPI.Data;
using TournamentAPI.Tournaments;
using TournamentMutations = TournamentAPI.Shared.MutationExamples.Mutations.Tournaments;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class JoinTournamentContentionLoadTests : BaseLoadTest, IClassFixture<LatencyWebAppFactory>
{
    private const string OwnerPrefix = "joinowner";
    private const string JoinerPrefix = "joiner";
    private const string TournamentNamePrefix = "Join Contention Tournament";
    private const int Joiners = 40;
    private const int Tournaments = 120;
    private const int MaxParticipants = 3;
    private const int ContendersPerTournament = 8;
    private const double MinSuccessfulJoinPercent = 5;

    private static readonly string[] ExpectedRejections =
    [
        TournamentErrorCodes.TournamentFull,
        TournamentErrorCodes.UserAlreadyParticipant
    ];

    private readonly LatencyWebAppFactory _factory;

    public JoinTournamentContentionLoadTests(LatencyWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JoinTournament_ShouldNeverOverfillATournament_UnderContention()
    {
        // Arrange
        VirtualUserSession[] sessions = [];
        IReadOnlyList<int> tournamentIds = [];

        var scenario = GraphQLLoadScenario.Create("join_tournament_contention", async context =>
        {
            var session = VirtualUserSessions.Pick(sessions, context.InvocationNumber);
            var tournamentId = tournamentIds[(int)(context.InvocationNumber / ContendersPerTournament % tournamentIds.Count)];

            return await GraphQLLoadStep.RunAsync(
                StepNames.JoinTournament,
                context,
                session.Client,
                TournamentMutations.JoinTournament,
                new { input = new { tournamentId } },
                GraphQLLoadStep.TrueResult("joinTournament"),
                expectedRejections: ExpectedRejections.ToHashSet(),
                bearerToken: session.AccessToken);
        })
        .WithInit(async _ =>
        {
            var owners = await WriteScenarioProvisioner.CreateUsersAsync(
                _factory.Services, OwnerPrefix, 1, _factory.UserPassword);

            var joiners = await WriteScenarioProvisioner.CreateUsersAsync(
                _factory.Services, JoinerPrefix, Joiners, _factory.UserPassword);

            tournamentIds = await WriteScenarioProvisioner.CreateOpenTournamentsAsync(
                _factory.Services, owners[0], TournamentNamePrefix, Tournaments, MaxParticipants);

            sessions = await VirtualUserSessions.LoginAsync(CreateClient, joiners, _factory.UserPassword, Joiners);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(Simulation.Inject(
            rate: LoadTestBudgets.Rates.JoinTournamentPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(15)));

        var thresholds = new List<LoadThreshold>
        {
            LoadThresholds.ErrorBudget(0),
            LoadThresholds.StatusMix("200", MinSuccessfulJoinPercent, [])
        };
        thresholds.AddRange(LoadThresholds.ForBudget(LoadTestBudgets.Steps.For(LoadTestDataSize.Default, StepNames.JoinTournament)));

        // Act
        LoadTestRun.Execute(scenario, thresholds.ToArray());

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var overfilled = await context.Tournaments
            .AsNoTracking()
            .Where(t => tournamentIds.Contains(t.Id) && t.Participants.Count > t.MaxParticipants)
            .Select(t => t.Id)
            .ToListAsync();

        Assert.Empty(overfilled);

        var joined = await context.Tournaments
            .AsNoTracking()
            .Where(t => tournamentIds.Contains(t.Id))
            .SumAsync(t => t.Participants.Count - 1);

        Assert.True(joined > 0, "No join succeeded, so the contention run proved nothing.");
    }
}
