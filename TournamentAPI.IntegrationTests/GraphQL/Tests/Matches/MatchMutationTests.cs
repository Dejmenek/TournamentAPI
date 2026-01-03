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
                winnerId = 1
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
                winnerId = winnerId
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
                winnerId = winnerId
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
                winnerId = winnerId
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
                winnerId = winnerId
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
    }
}
