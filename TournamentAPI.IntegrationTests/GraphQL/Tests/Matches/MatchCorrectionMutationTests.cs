using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;
using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Matches;

public class MatchCorrectionMutationTests : BaseIntegrationTest
{
    public MatchCorrectionMutationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    private async Task<TestClient> LoginAsync(string email, string password = "Password123!")
    {
        var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email, password } });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        return client;
    }

    private async Task<string> GetVersionAsync(int matchId)
    {
        var match = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == matchId);
        return MatchVersionCodec.Encode(match.RowVersion);
    }

    private static object CorrectMatchResultVariables(int matchId, int winnerId, int player1Score, int player2Score, string version)
        => new { input = new { matchId, winnerId, player1Score, player2Score, version } };

    [Fact]
    public async Task CorrectMatchResult_ScoreOnlyCorrection_DoesNotChangeWinner_AndDoesNotCascade()
    {
        // Arrange
        var matchId = 2; // tournament 3, round 1: carol vs david, winner david
        var winnerId = 4; // david (unchanged)
        using var client = await LoginAsync("alice@example.com");
        var version = await GetVersionAsync(matchId);

        // Act
        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, winnerId, 1, 5, version));

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CorrectMatchResult);
        Assert.True(response.Data.CorrectMatchResult.Boolean);

        var match = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == matchId);
        Assert.Equal(winnerId, match.WinnerId);
        Assert.Equal(1, match.Player1Score);
        Assert.Equal(5, match.Player2Score);
        Assert.Equal(MatchStatus.Played, match.Status);

        var downstreamMatch = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == 5);
        Assert.Equal(1, downstreamMatch.Player1Id);
        Assert.Equal(4, downstreamMatch.Player2Id);
        Assert.Equal(1, downstreamMatch.WinnerId);
        Assert.Equal(MatchStatus.Played, downstreamMatch.Status);

        var audits = await DbContext.MatchCorrectionAudits.AsNoTracking().Where(a => a.MatchId == matchId).ToListAsync();
        Assert.Single(audits);
        Assert.Null(audits[0].TriggeredByMatchId);
    }

    [Fact]
    public async Task CorrectMatchResult_WinnerChanges_WhenNoDownstreamRoundGenerated_DoesNotCascade()
    {
        // Arrange
        var matchId = 14; // tournament 7, round 1 only: alice vs bob, winner alice
        var winnerId = 2; // bob
        using var client = await LoginAsync("carol@example.com");
        var version = await GetVersionAsync(matchId);

        // Act
        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, winnerId, 1, 3, version));

        // Assert
        Assert.False(response.HasErrors);
        Assert.True(response.Data!.CorrectMatchResult!.Boolean);

        var match = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == matchId);
        Assert.Equal(winnerId, match.WinnerId);
        Assert.Equal(1, match.Player1Score);
        Assert.Equal(3, match.Player2Score);

        var audits = await DbContext.MatchCorrectionAudits.AsNoTracking().Where(a => a.MatchId == matchId).ToListAsync();
        Assert.Single(audits);
    }

    [Fact]
    public async Task CorrectMatchResult_WinnerChanges_WhenDownstreamAlreadyPlayed_FlipsDownstreamToNeedsReplay_AndLeavesTwoHopsAwayUntouched()
    {
        // Arrange
        var matchId = 1; // tournament 3, round 1: alice vs bob, winner alice
        var winnerId = 2; // bob
        using var client = await LoginAsync("alice@example.com");
        var version = await GetVersionAsync(matchId);

        // Act
        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, winnerId, 1, 3, version));

        // Assert
        Assert.False(response.HasErrors);
        Assert.True(response.Data!.CorrectMatchResult!.Boolean);

        var match5 = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == 5);
        Assert.Equal(2, match5.Player1Id); // bob swapped in for alice
        Assert.Equal(4, match5.Player2Id); // david, unchanged
        Assert.Equal(1, match5.WinnerId); // stale winner (alice) preserved
        Assert.Equal(MatchStatus.NeedsReplay, match5.Status);

        var match7 = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == 7);
        Assert.Equal(1, match7.Player1Id);
        Assert.Equal(5, match7.Player2Id);
        Assert.Equal(1, match7.WinnerId);
        Assert.Equal(MatchStatus.Played, match7.Status);
    }

    [Fact]
    public async Task Play_ReplayingNeedsReplayMatch_WithGenuinelyDifferentWinner_CascadesToNextHop()
    {
        // Arrange: correct match1 so match5 becomes NeedsReplay (alice swapped out for bob)
        using var ownerClient = await LoginAsync("alice@example.com");
        var match1Version = await GetVersionAsync(1);
        await ownerClient.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(1, 2, 1, 3, match1Version));

        // Act: replay match5 (now bob vs david) with david as the genuinely different winner
        var response = await ownerClient.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new
            {
                input = new
                {
                    matchId = 5,
                    winnerId = 4, // david
                    player1Score = 1,
                    player2Score = 3
                }
            });

        // Assert
        Assert.False(response.HasErrors);
        Assert.True(response.Data!.Play!.Boolean);

        var match5 = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == 5);
        Assert.Equal(4, match5.WinnerId);
        Assert.Equal(MatchStatus.Played, match5.Status);

        var match7 = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == 7);
        Assert.Equal(4, match7.Player1Id); // david swapped in for the stale winner (alice)
        Assert.Equal(5, match7.Player2Id); // emma, unchanged
        Assert.Equal(1, match7.WinnerId); // stale winner (alice) preserved
        Assert.Equal(MatchStatus.NeedsReplay, match7.Status);
    }

    [Fact]
    public async Task CorrectMatchResult_WhenDownstreamIsStillScheduled_OnlySwapsSlot_AndDoesNotChangeStatus()
    {
        // Arrange: build a custom bracket where round 2 has been generated but not yet played
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "alice");
        var bob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "bob");
        var carol = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "carol");
        var david = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "david");

        var tournament = new Tournament
        {
            Name = "Scheduled Downstream Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            Status = TournamentStatus.Closed,
            OwnerId = alice.Id,
            MaxParticipants = 4
        };
        var bracket = new Bracket { Tournament = tournament };
        var matchA = new Match { Round = 1, Player1Id = alice.Id, Player2Id = bob.Id, WinnerId = alice.Id, Status = MatchStatus.Played, Bracket = bracket };
        var matchB = new Match { Round = 1, Player1Id = carol.Id, Player2Id = david.Id, WinnerId = carol.Id, Status = MatchStatus.Played, Bracket = bracket };

        DbContext.Tournaments.Add(tournament);
        DbContext.Matches.AddRange(matchA, matchB);
        await DbContext.SaveChangesAsync();

        var matchC = new Match
        {
            Round = 2,
            BracketId = bracket.Id,
            Player1Id = Math.Min(alice.Id, carol.Id),
            Player2Id = Math.Max(alice.Id, carol.Id),
            WinnerId = null,
            Status = MatchStatus.Scheduled
        };
        DbContext.Matches.Add(matchC);
        await DbContext.SaveChangesAsync();

        using var client = await LoginAsync("alice@example.com");
        var version = await GetVersionAsync(matchA.Id);

        // Act: correct matchA's winner from alice to bob
        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchA.Id, bob.Id, 1, 3, version));

        // Assert
        Assert.False(response.HasErrors);
        Assert.True(response.Data!.CorrectMatchResult!.Boolean);

        var downstream = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == matchC.Id);
        Assert.Equal(bob.Id, downstream.Player1Id);
        Assert.Equal(carol.Id, downstream.Player2Id);
        Assert.Null(downstream.WinnerId);
        Assert.Equal(MatchStatus.Scheduled, downstream.Status);
    }

    [Fact]
    public async Task CorrectMatchResult_WhenDownstreamIsABye_AutoAdvancesAndContinuesTheWalk()
    {
        // Arrange: 6-participant bracket where round 1's third match feeds a round 2 bye,
        // which in turn feeds round 3's final.
        var alice = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "alice");
        var bob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "bob");
        var carol = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "carol");
        var david = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "david");
        var emma = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "emma");
        var frank = await DbContext.Users.AsNoTracking().FirstAsync(u => u.UserName == "frank");

        var tournament = new Tournament
        {
            Name = "Bye Downstream Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            Status = TournamentStatus.Closed,
            OwnerId = alice.Id,
            MaxParticipants = 6
        };
        var bracket = new Bracket { Tournament = tournament };
        var match1 = new Match { Round = 1, Player1Id = alice.Id, Player2Id = bob.Id, WinnerId = alice.Id, Status = MatchStatus.Played, Bracket = bracket };
        var match2 = new Match { Round = 1, Player1Id = carol.Id, Player2Id = david.Id, WinnerId = carol.Id, Status = MatchStatus.Played, Bracket = bracket };
        var match3 = new Match { Round = 1, Player1Id = emma.Id, Player2Id = frank.Id, WinnerId = emma.Id, Status = MatchStatus.Played, Bracket = bracket };

        DbContext.Tournaments.Add(tournament);
        DbContext.Matches.AddRange(match1, match2, match3);
        await DbContext.SaveChangesAsync();

        var round2Pos0 = new Match
        {
            Round = 2,
            BracketId = bracket.Id,
            Player1Id = Math.Min(alice.Id, carol.Id),
            Player2Id = Math.Max(alice.Id, carol.Id),
            WinnerId = alice.Id,
            Status = MatchStatus.Played
        };
        var round2Bye = new Match
        {
            Round = 2,
            BracketId = bracket.Id,
            Player1Id = emma.Id,
            Player2Id = null,
            WinnerId = emma.Id,
            Status = MatchStatus.Played
        };

        DbContext.Matches.AddRange(round2Pos0, round2Bye);
        await DbContext.SaveChangesAsync();

        var final = new Match
        {
            Round = 3,
            BracketId = bracket.Id,
            Player1Id = Math.Min(alice.Id, emma.Id),
            Player2Id = Math.Max(alice.Id, emma.Id),
            WinnerId = emma.Id,
            Status = MatchStatus.Played
        };
        DbContext.Matches.Add(final);
        await DbContext.SaveChangesAsync();

        using var client = await LoginAsync("alice@example.com");
        var version = await GetVersionAsync(match3.Id);

        // Act: correct match3's winner from emma to frank
        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(match3.Id, frank.Id, 1, 3, version));

        // Assert
        Assert.False(response.HasErrors);
        Assert.True(response.Data!.CorrectMatchResult!.Boolean);

        var byeAfter = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == round2Bye.Id);
        Assert.Equal(frank.Id, byeAfter.Player1Id);
        Assert.Null(byeAfter.Player2Id);
        Assert.Equal(frank.Id, byeAfter.WinnerId); // auto-advanced
        Assert.Equal(MatchStatus.Played, byeAfter.Status);

        var finalAfter = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == final.Id);
        Assert.Equal(alice.Id, finalAfter.Player1Id);
        Assert.Equal(frank.Id, finalAfter.Player2Id); // emma swapped out for frank
        Assert.Equal(emma.Id, finalAfter.WinnerId); // stale winner preserved
        Assert.Equal(MatchStatus.NeedsReplay, finalAfter.Status); // continued the walk into round 3
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsMatchNotFoundError_WhenMatchDoesNotExist()
    {
        var matchId = 99999;
        using var client = await LoginAsync("alice@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 1, 3, 1, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.MatchNotFound(matchId);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsTournamentNotOwnerError_WhenUserIsNotOwner()
    {
        var matchId = 14; // owned by carol
        using var client = await LoginAsync("alice@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 1, 3, 1, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = TournamentErrors.TournamentNotOwner(1, 7);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsTournamentNotClosedError_WhenTournamentIsNotClosed()
    {
        var matchId = 14;
        var tournamentId = 7;
        var tournament = await DbContext.Tournaments.FirstAsync(t => t.Id == tournamentId);
        tournament.Status = TournamentStatus.Open;
        await DbContext.SaveChangesAsync();

        using var client = await LoginAsync("carol@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 1, 3, 1, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.TournamentNotClosed(tournamentId);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsMatchNotYetPlayedError_WhenMatchIsScheduled()
    {
        var matchId = 9; // tournament 4, scheduled
        using var client = await LoginAsync("carol@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 5, 3, 1, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.MatchNotYetPlayed(matchId);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsMatchNeedsReplayError_WhenMatchNeedsReplay()
    {
        // Arrange: create a NeedsReplay match via a cascade
        using var ownerClient = await LoginAsync("alice@example.com");
        var match1Version = await GetVersionAsync(1);
        await ownerClient.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(1, 2, 1, 3, match1Version));

        // Act: attempt to correct match5, which is now NeedsReplay
        var response = await ownerClient.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(5, 4, 1, 3, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.MatchNeedsReplay(5);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsInvalidMatchWinnerError_WhenWinnerIsNotMatchParticipant()
    {
        var matchId = 14;
        using var client = await LoginAsync("carol@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 5, 3, 1, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.InvalidMatchWinner(matchId, 5);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsNegativeScoreError_WhenScoreIsNegative()
    {
        var matchId = 14;
        using var client = await LoginAsync("carol@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 1, -1, 3, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.NegativeScore(matchId, -1, 3);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsWinnerScoreMismatchError_WhenWinnerScoreIsNotHigher()
    {
        var matchId = 14;
        using var client = await LoginAsync("carol@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 1, 1, 3, "dummy"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.WinnerScoreMismatch(matchId, 1);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsInvalidVersionTokenError_WhenVersionIsMalformed()
    {
        var matchId = 14;
        using var client = await LoginAsync("carol@example.com");

        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 1, 3, 1, "not-valid-base64!!!"));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.InvalidVersionToken(matchId);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ReturnsVersionConflictError_WhenTokenIsStaleAndTargetDiffersFromCommittedState()
    {
        var matchId = 15; // tournament 7: grace vs henry, winner grace
        using var client = await LoginAsync("carol@example.com");
        var staleVersion = await GetVersionAsync(matchId);

        // First correction lands and moves the row's version forward.
        var firstResponse = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 7, 5, 1, staleVersion));
        Assert.False(firstResponse.HasErrors);

        // Second call reuses the now-stale version and asks for a different target.
        var response = await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(matchId, 8, 1, 5, staleVersion));

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = MatchErrors.MatchVersionConflict(matchId);
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
    }

    [Fact]
    public async Task CorrectMatchResult_ConcurrentIdenticalRequests_BothSucceed_OneRealWriteOneIdempotentNoOp()
    {
        // Arrange
        var matchId = 16; // tournament 12: alice vs bob, winner bob (unchanged target)
        using var client1 = await LoginAsync("alice@example.com");
        using var client2 = await LoginAsync("alice@example.com");
        var version = await GetVersionAsync(matchId);
        var variables = CorrectMatchResultVariables(matchId, 2, 1, 5, version);

        // Act
        var task1 = client1.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult, variables);
        var task2 = client2.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult, variables);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.All(results, r => Assert.False(r.HasErrors));
        Assert.All(results, r => Assert.True(r.Data!.CorrectMatchResult!.Boolean));

        var match = await DbContext.Matches.AsNoTracking().FirstAsync(m => m.Id == matchId);
        Assert.Equal(2, match.WinnerId);
        Assert.Equal(1, match.Player1Score);
        Assert.Equal(5, match.Player2Score);
    }

    [Fact]
    public async Task CorrectMatchResult_CascadeAuditRows_ShareOneCorrelationId_AndChainViaTriggeredByMatchId()
    {
        // Arrange
        using var client = await LoginAsync("alice@example.com");
        var version = await GetVersionAsync(1);

        // Act
        await client.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(1, 2, 1, 3, version));

        // Assert
        var sourceAudit = await DbContext.MatchCorrectionAudits.AsNoTracking().SingleAsync(a => a.MatchId == 1);
        var cascadeAudit = await DbContext.MatchCorrectionAudits.AsNoTracking().SingleAsync(a => a.MatchId == 5);

        Assert.Null(sourceAudit.TriggeredByMatchId);
        Assert.Equal(1, cascadeAudit.TriggeredByMatchId);
        Assert.Equal(sourceAudit.CorrelationId, cascadeAudit.CorrelationId);
    }

    [Fact]
    public async Task ChampionSelfHealing_DropsChampionWhileFinalNeedsReplay_ThenReflectsNewChampionAfterReplay()
    {
        // Arrange: correct match1 (alice -> bob), invalidating match5, then replay match5 with david,
        // which cascades into the final (match7), invalidating alice's championship.
        using var ownerClient = await LoginAsync("alice@example.com");
        var match1Version = await GetVersionAsync(1);
        await ownerClient.ExecuteMutationAsync<CorrectMatchResultResponse>(
            Shared.MutationExamples.Mutations.Match.CorrectMatchResult,
            CorrectMatchResultVariables(1, 2, 1, 3, match1Version));

        await ownerClient.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = 5, winnerId = 4, player1Score = 1, player2Score = 3 } });

        // Act (part 1): the final (match7) is now NeedsReplay - alice should no longer be champion
        using var aliceClient = await LoginAsync("alice@example.com");
        var midResponse = await aliceClient.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMeWithTournamentHistory);

        Assert.False(midResponse.HasErrors);
        var midWonNames = midResponse.Data!.Me!.WonTournaments!.Nodes!.Select(t => t.Name).ToList();
        Assert.DoesNotContain("Winter Championship 2024", midWonNames);

        // Act (part 2): replay the final with emma as the new champion
        await ownerClient.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            new { input = new { matchId = 7, winnerId = 5, player1Score = 1, player2Score = 3 } });

        using var emmaClient = await LoginAsync("emma@example.com");
        var finalResponse = await emmaClient.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMeWithTournamentHistory);

        // Assert
        Assert.False(finalResponse.HasErrors);
        var emmaWonNames = finalResponse.Data!.Me!.WonTournaments!.Nodes!.Select(t => t.Name).ToList();
        Assert.Contains("Winter Championship 2024", emmaWonNames);
    }
}
