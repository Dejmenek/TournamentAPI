using Microsoft.EntityFrameworkCore;
using TournamentAPI.Matches;
using TournamentAPI.Shared.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Matches;
public class MatchMutationTests : BaseIntegrationTest
{
    public MatchMutationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Play_HandlesDbUpdateException_WhenRaceConditionOccurs()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var matchId = 10;
        var winnerId = 8;
        var player1Score = 1;
        var player2Score = 3;
        using var client1 = CreateClient();
        using var client2 = CreateClient();

        var tokenResponse = await client1.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client1.SetAuthToken(tokenResponse.Data.LoginUser.String);
        client2.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId,
                player1Score = player1Score,
                player2Score = player2Score
            }
        };

        // Act
        var task1 = client1.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);
        var task2 = client2.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        var successResponse = results.FirstOrDefault(r => !r.HasErrors);
        var failureResponse = results.FirstOrDefault(r => r.HasErrors);

        Assert.NotNull(successResponse);
        Assert.NotNull(failureResponse);
        Assert.NotNull(failureResponse.Data);
        Assert.NotNull(failureResponse.Data.Play);
        Assert.Null(failureResponse.Data.Play.Boolean);
        Assert.NotNull(failureResponse.Errors);

        var error = failureResponse.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = MatchErrors.MatchAlreadyPlayed(matchId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["MatchId"]!.ToString(), error.Extensions["MatchId"]?.ToString());
    }

    [Fact]
    public async Task Play_ReturnsMatchNotFoundError_WhenMatchDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var matchId = 999;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = 1,
                player1Score = 3,
                player2Score = 1
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = MatchErrors.MatchNotFound(matchId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["MatchId"]?.ToString(), error.Extensions["MatchId"]?.ToString());
    }

    [Fact]
    public async Task Play_ReturnsTournamentNotOnwerError_WhenUserIsNotTournamentOwner()
    {
        // Arrange
        var email = "david@example.com";
        var password = "Password123!";
        var tournamentId = 4;
        var matchId = 9;
        var winnerId = 5;
        var player1Score = 3;
        var player2Score = 1;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId,
                player1Score = player1Score,
                player2Score = player2Score
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotOwner(4, tournamentId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var match = await DbContext.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId);

        Assert.NotNull(match);
        Assert.Null(match.WinnerId);
    }

    [Fact]
    public async Task Play_ReturnsMatchAlreadyPlayedError_WhenMatchHasAlreadyBeenPlayed()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var matchId = 8;
        var winnerId = 2;
        var player1Score = 3;
        var player2Score = 1;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId,
                player1Score = player1Score,
                player2Score = player2Score
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = MatchErrors.MatchAlreadyPlayed(matchId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["MatchId"]?.ToString(), error.Extensions["MatchId"]?.ToString());
    }

    [Fact]
    public async Task Play_ReturnsInvalidMatchWinnerError_WhenWinnerIsNotMatchParticipant()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var matchId = 10;
        var winnerId = 2;
        var player1Score = 3;
        var player2Score = 1;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId,
                player1Score = player1Score,
                player2Score = player2Score
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = MatchErrors.InvalidMatchWinner(matchId, winnerId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["MatchId"]?.ToString(), error.Extensions["MatchId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["WinnerId"]?.ToString(), error.Extensions["WinnerId"]?.ToString());

        var match = await DbContext.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId);

        Assert.NotNull(match);
        Assert.Null(match.WinnerId);
    }

    [Fact]
    public async Task Play_Succeeds_WhenInputIsValid()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var matchId = 9;
        var winnerId = 5;
        var player1Score = 3;
        var player2Score = 1;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId,
                player1Score = player1Score,
                player2Score = player2Score
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.True(response.Data.Play.Boolean);

        var match = await DbContext.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId);

        Assert.NotNull(match);
        Assert.Equal(winnerId, match.WinnerId);
        Assert.Equal(player1Score, match.Player1Score);
        Assert.Equal(player2Score, match.Player2Score);
    }

    [Fact]
    public async Task Play_ReturnsNegativeScoreError_WhenScoreIsNegative()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var matchId = 9;
        var winnerId = 5;
        var player1Score = -1;
        var player2Score = 1;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId,
                player1Score = player1Score,
                player2Score = player2Score
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = MatchErrors.NegativeScore(matchId, player1Score, player2Score);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["MatchId"]?.ToString(), error.Extensions["MatchId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["Player1Score"]?.ToString(), error.Extensions["Player1Score"]?.ToString());
        Assert.Equal(expectedError.Extensions!["Player2Score"]?.ToString(), error.Extensions["Player2Score"]?.ToString());

        var match = await DbContext.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId);

        Assert.NotNull(match);
        Assert.Null(match.WinnerId);
    }

    [Fact]
    public async Task Play_ReturnsWinnerScoreMismatchError_WhenWinnerScoreIsNotHigher()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var matchId = 9;
        var winnerId = 5;
        var player1Score = 2;
        var player2Score = 2;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId,
                player1Score = player1Score,
                player2Score = player2Score
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<PlayMatchResponse>(
            Shared.MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = MatchErrors.WinnerScoreMismatch(matchId, winnerId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["MatchId"]?.ToString(), error.Extensions["MatchId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["WinnerId"]?.ToString(), error.Extensions["WinnerId"]?.ToString());

        var match = await DbContext.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId);

        Assert.NotNull(match);
        Assert.Null(match.WinnerId);
    }
}
