using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using TournamentAPI.Brackets;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using BracketMutations = TournamentAPI.Shared.MutationExamples.Mutations.Bracket;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class BracketGenerationLoadTests : BaseLoadTest, IClassFixture<LatencyWebAppFactory>
{
    private const int TournamentsPerScenario = 24;
    private const int CallsPerTournament = 2;

    private static readonly int[] ParticipantCounts = [8, 16, 32];
    private static readonly TournamentFormat[] Formats = [TournamentFormat.SingleElimination, TournamentFormat.RoundRobin];

    private readonly LatencyWebAppFactory _factory;

    public BracketGenerationLoadTests(LatencyWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateBracket_ShouldMeetBudgets_AndCreateOneBracketPerTournament()
    {
        // Arrange
        var owners = await WriteScenarioProvisioner.CreateUsersAsync(_factory.Services, "bracketowner", 1, _factory.UserPassword);
        var players = await WriteScenarioProvisioner.CreateUsersAsync(_factory.Services, "bracketplayer", ParticipantCounts.Max(), _factory.UserPassword);
        var owner = (await VirtualUserSessions.LoginAsync(CreateClient, owners, _factory.UserPassword, 1))[0];

        var runs = new List<ScenarioRun>();
        var tournamentIds = new List<int>();

        foreach (var format in Formats)
        {
            foreach (var participantCount in ParticipantCounts)
            {
                var stepName = StepNames.GenerateBracket(format, participantCount);

                var ids = await WriteScenarioProvisioner.CreateClosedTournamentsAsync(
                    _factory.Services, owners[0], players, $"Bracket {stepName}", TournamentsPerScenario, participantCount, format);

                tournamentIds.AddRange(ids);

                var scenario = GraphQLLoadScenario.Create(stepName, async context =>
                    await GraphQLLoadStep.RunAsync(
                        stepName,
                        context,
                        owner.Client,
                        BracketMutations.GenerateBracket,
                        new { input = new { tournamentId = ids[(int)(context.InvocationNumber / CallsPerTournament % ids.Count)] } },
                        GraphQLLoadStep.NonNullField("generateBracket"),
                        expectedRejections: new HashSet<string> { BracketErrorCodes.BracketAlreadyExists },
                        bearerToken: owner.AccessToken))
                .WithLoadSimulations(Simulation.Inject(
                    rate: LoadTestBudgets.Rates.GenerateBracketPerSecond,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromSeconds(10)));

                var thresholds = new List<LoadThreshold>
                {
                    LoadThresholds.ErrorBudget(0),
                    LoadThresholds.StatusMix("200", 100.0 / CallsPerTournament - 1, [])
                };
                thresholds.AddRange(LoadThresholds.ForBudget(LoadTestBudgets.Steps.For(LoadTestDataSize.Default, stepName)));

                runs.Add(new ScenarioRun(scenario, thresholds.ToArray()));
            }
        }

        // Act
        var stats = LoadTestRun.ExecuteAll(runs.ToArray());

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var withBracket = await context.Tournaments
            .AsNoTracking()
            .CountAsync(t => tournamentIds.Contains(t.Id) && t.Bracket != null);

        var successful = stats.Sum(s => SuccessfulRequests(s.Value, s.Key));

        Assert.Equal(successful, withBracket);
    }

    private static int SuccessfulRequests(ScenarioStats stats, string step)
        => stats.StepStats.Get(step).Ok.StatusCodes.Find("200")?.Count ?? 0;
}
