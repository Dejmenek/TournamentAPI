using TournamentAPI.IntegrationTests.GraphQL.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Matches;
public class MatchMutationTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public MatchMutationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Play_ReturnsMatchNotFoundError_WhenMatchDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var matchId = 999;
        var tokenResponse = await _fixture.Client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        _fixture.Client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = 1
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<PlayMatchResponse>(
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
        var tokenResponse = await _fixture.Client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        _fixture.Client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<PlayMatchResponse>(
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

        using var context = _fixture.CreateDbContext();
        var match = await context.Matches.FindAsync(matchId);

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
        var tokenResponse = await _fixture.Client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        _fixture.Client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<PlayMatchResponse>(
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
        var matchId = 9;
        var winnerId = 2;
        var tokenResponse = await _fixture.Client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        _fixture.Client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<PlayMatchResponse>(
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

        using var context = _fixture.CreateDbContext();
        var match = await context.Matches.FindAsync(matchId);

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
        var tokenResponse = await _fixture.Client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });
        _fixture.Client.SetAuthToken(tokenResponse.Data.LoginUser.String);

        var variables = new
        {
            input = new
            {
                matchId = matchId,
                winnerId = winnerId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<PlayMatchResponse>(
            MutationExamples.Mutations.Match.Play,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Play);
        Assert.True(response.Data.Play.Boolean);
        Assert.Null(response.Data.Play.Errors);

        using var context = _fixture.CreateDbContext();
        var match = await context.Matches.FindAsync(matchId);

        Assert.NotNull(match);
        Assert.Equal(winnerId, match.WinnerId);
    }
}
