using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;
using MatchMutations = TournamentAPI.Shared.MutationExamples.Mutations.Match;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class MatchContentionLoadTests : BaseLoadTest, IClassFixture<LatencyWebAppFactory>
{
    private const int Players = 8;
    private const int Matches = 120;
    private const int ContendersPerMatch = 6;

    private readonly LatencyWebAppFactory _factory;

    public MatchContentionLoadTests(LatencyWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlayMatch_ShouldAcceptExactlyOneResult_UnderContention()
    {
        // Arrange
        VirtualUserSession[] owner = [];
        IReadOnlyList<ProvisionedMatch> matches = [];

        var scenario = GraphQLLoadScenario.Create("play_match_contention", async context =>
        {
            var target = matches[(int)(context.InvocationNumber / ContendersPerMatch % matches.Count)];
            var contender = (int)(context.InvocationNumber % ContendersPerMatch);

            return await GraphQLLoadStep.RunAsync(
                StepNames.PlayMatch,
                context,
                owner[0].Client,
                MatchMutations.Play,
                new
                {
                    input = new
                    {
                        matchId = target.Id,
                        winnerId = target.Player1Id,
                        player1Score = 2 + contender,
                        player2Score = 0
                    }
                },
                GraphQLLoadStep.TrueResult("play"),
                expectedRejections: new HashSet<string> { MatchErrorCodes.MatchAlreadyPlayed },
                bearerToken: owner[0].AccessToken);
        })
        .WithInit(async _ =>
        {
            var (ownerEmail, playerEmails) = await CreateOwnerAndPlayersAsync("playowner", "playplayer");

            matches = await WriteScenarioProvisioner.CreateClosedTournamentMatchesAsync(
                _factory.Services, ownerEmail, playerEmails, "Play Contention Tournament", Matches, played: false);

            owner = await VirtualUserSessions.LoginAsync(CreateClient, [ownerEmail], _factory.UserPassword, 1);
        })
        .WithLoadSimulations(ContentionRate());

        // Act
        var stats = LoadTestRun.Execute(scenario, ContentionThresholds(StepNames.PlayMatch));

        // Assert
        var matchIds = matches.Select(m => m.Id).ToList();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var played = await context.Matches
            .AsNoTracking()
            .CountAsync(m => matchIds.Contains(m.Id) && m.Status == MatchStatus.Played);

        Assert.Equal(SuccessfulRequests(stats, StepNames.PlayMatch), played);
    }

    [Fact]
    public async Task CorrectMatchResult_ShouldAcceptExactlyOneCorrection_PerStaleVersion()
    {
        // Arrange
        VirtualUserSession[] owner = [];
        IReadOnlyList<ProvisionedMatch> matches = [];

        var scenario = GraphQLLoadScenario.Create("correct_match_result_contention", async context =>
        {
            var target = matches[(int)(context.InvocationNumber / ContendersPerMatch % matches.Count)];
            var contender = (int)(context.InvocationNumber % ContendersPerMatch);

            return await GraphQLLoadStep.RunAsync(
                StepNames.CorrectMatchResult,
                context,
                owner[0].Client,
                MatchMutations.CorrectMatchResult,
                new
                {
                    input = new
                    {
                        matchId = target.Id,
                        winnerId = target.Player1Id,
                        player1Score = WriteScenarioProvisioner.PlayedMatchPlayer1Score + 1 + contender,
                        player2Score = 0,
                        version = target.Version
                    }
                },
                GraphQLLoadStep.TrueResult("correctMatchResult"),
                expectedRejections: new HashSet<string> { MatchErrorCodes.MatchVersionConflict },
                bearerToken: owner[0].AccessToken);
        })
        .WithInit(async _ =>
        {
            var (ownerEmail, playerEmails) = await CreateOwnerAndPlayersAsync("correctowner", "correctplayer");

            matches = await WriteScenarioProvisioner.CreateClosedTournamentMatchesAsync(
                _factory.Services, ownerEmail, playerEmails, "Correction Contention Tournament", Matches, played: true);

            owner = await VirtualUserSessions.LoginAsync(CreateClient, [ownerEmail], _factory.UserPassword, 1);
        })
        .WithLoadSimulations(ContentionRate());

        // Act
        var stats = LoadTestRun.Execute(scenario, ContentionThresholds(StepNames.CorrectMatchResult));

        // Assert
        var matchIds = matches.Select(m => m.Id).ToList();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var corrected = await context.Matches
            .AsNoTracking()
            .CountAsync(m => matchIds.Contains(m.Id) && m.Player1Score != WriteScenarioProvisioner.PlayedMatchPlayer1Score);

        Assert.Equal(SuccessfulRequests(stats, StepNames.CorrectMatchResult), corrected);
    }

    private async Task<(string OwnerEmail, IReadOnlyList<string> PlayerEmails)> CreateOwnerAndPlayersAsync(
        string ownerPrefix,
        string playerPrefix)
    {
        var owners = await WriteScenarioProvisioner.CreateUsersAsync(_factory.Services, ownerPrefix, 1, _factory.UserPassword);
        var players = await WriteScenarioProvisioner.CreateUsersAsync(_factory.Services, playerPrefix, Players, _factory.UserPassword);

        return (owners[0], players);
    }

    private static LoadSimulation ContentionRate()
    {
        return Simulation.Inject(
            rate: LoadTestBudgets.Rates.MatchContentionPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(15));
    }

    private static LoadThreshold[] ContentionThresholds(string step)
    {
        var thresholds = new List<LoadThreshold>
        {
            LoadThresholds.ErrorBudget(0),
            LoadThresholds.StatusMix("200", 100.0 / ContendersPerMatch - 1, [])
        };
        thresholds.AddRange(LoadThresholds.ForBudget(LoadTestBudgets.Steps.For(LoadTestDataSize.Default, step)));

        return thresholds.ToArray();
    }

    private static int SuccessfulRequests(ScenarioStats stats, string step)
        => stats.StepStats.Get(step).Ok.StatusCodes.Find("200")?.Count ?? 0;
}
