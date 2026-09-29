using NBomber.CSharp;
using TournamentMutations = TournamentAPI.Shared.MutationExamples.Mutations.Tournaments;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class CreateTournamentLoadTests : BaseLoadTest, IClassFixture<LatencyWebAppFactory>
{
    private const string UserPrefix = "creator";
    private const int Creators = 10;

    private readonly LatencyWebAppFactory _factory;

    public CreateTournamentLoadTests(LatencyWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public void CreateTournament_ShouldMeetBudgets_UnderSteadyWrites()
    {
        // Arrange
        VirtualUserSession[] sessions = [];

        var scenario = GraphQLLoadScenario.Create("create_tournament", async context =>
        {
            var session = VirtualUserSessions.Pick(sessions, context.InvocationNumber);

            return await GraphQLLoadStep.RunAsync(
                StepNames.CreateTournament,
                context,
                session.Client,
                TournamentMutations.CreateTournamentWithBasicFieldsReturn,
                new
                {
                    input = new
                    {
                        name = $"Write Load Tournament {context.InvocationNumber}",
                        startDate = DateTime.UtcNow.AddDays(7).ToString("o"),
                        status = "OPEN",
                        maxParticipants = 8
                    }
                },
                GraphQLLoadStep.NonNullField("createTournament"),
                bearerToken: session.AccessToken);
        })
        .WithInit(async _ =>
        {
            var emails = await WriteScenarioProvisioner.CreateUsersAsync(
                _factory.Services, UserPrefix, Creators, _factory.UserPassword);

            sessions = await VirtualUserSessions.LoginAsync(CreateClient, emails, _factory.UserPassword, Creators);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(Simulation.Inject(
            rate: LoadTestBudgets.Rates.CreateTournamentPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(30)));

        // Act & Assert
        LoadTestRun.Execute(
            scenario,
            LoadThresholds.ForSteps(LoadTestDataSize.Default, "200", StepNames.CreateTournament));
    }
}
