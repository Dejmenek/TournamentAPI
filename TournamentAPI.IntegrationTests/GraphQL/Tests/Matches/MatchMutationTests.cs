using Microsoft.EntityFrameworkCore;
using TournamentAPI.Shared.Models;

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
            MutationExamples.Mutations.Users.LoginUser,
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
            MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Data.Play.Errors);

        var errorMessage = response.Data.Play.Errors.First().Message;
        Assert.Contains("Match was not found", errorMessage);
    }

    [Fact]
    public async Task Play_ReturnsTournamentNotOnwerError_WhenUserIsNotTournamentOwner()
    {
        // Arrange
        var email = "david@example.com";
        var password = "Password123!";
        var matchId = 9;
        var winnerId = 5;
        using var client = CreateClient();

        var tokenResponse = await client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
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
            MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Data.Play.Errors);

        var errorMessage = response.Data.Play.Errors.First().Message;
        Assert.Contains("User is not the owner of the tournament", errorMessage);

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
            MutationExamples.Mutations.Users.LoginUser,
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
            MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Data.Play.Errors);

        var errorMessage = response.Data.Play.Errors.First().Message;
        Assert.Contains("Match has already been played", errorMessage);
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
            MutationExamples.Mutations.Users.LoginUser,
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
            MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.Null(response.Data.Play.Boolean);
        Assert.NotNull(response.Data.Play.Errors);

        var errorMessage = response.Data.Play.Errors.First().Message;
        Assert.Contains("Winner must be one of the match participants", errorMessage);

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
            MutationExamples.Mutations.Users.LoginUser,
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
            MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.True(response.Data.Play.Boolean);
        Assert.Null(response.Data.Play.Errors);

        var match = await DbContext.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == matchId);

        Assert.NotNull(match);
        Assert.Equal(winnerId, match.WinnerId);
    }
}
