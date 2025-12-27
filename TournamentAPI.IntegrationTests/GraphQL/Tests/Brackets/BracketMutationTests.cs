using Microsoft.EntityFrameworkCore;
using TournamentAPI.IntegrationTests.GraphQL.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Brackets;
public class BracketMutationTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public BracketMutationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GenerateBracket_ReturnsTournamentNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 9999;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<GenerateBracketResponse>(
            MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Data.GenerateBracket.Errors);

        var errorMessage = response.Data.GenerateBracket.Errors.First().Message;
        Assert.Contains("Tournament doesn't exist", errorMessage);
    }

    [Fact]
    public async Task GenerateBracket_ReturnsTournamentNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 2;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<GenerateBracketResponse>(
            MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Data.GenerateBracket.Errors);

        var errorMessage = response.Data.GenerateBracket.Errors.First().Message;
        Assert.Contains("User is not the owner of the tournament", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var bracketInDb = await dbContext.Brackets
            .FirstOrDefaultAsync(b => b.TournamentId == tournamentCreateBracketId);

        Assert.Null(bracketInDb);
    }

    [Fact]
    public async Task GenerateBracket_ReturnsBracketGenerationNotAllowedError_WhenTournamentIsNotClosed()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 1;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<GenerateBracketResponse>(
            MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Data.GenerateBracket.Errors);

        var errorMessage = response.Data.GenerateBracket.Errors.First().Message;
        Assert.Contains("Bracket can only be generated when the tournament is closed", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var bracketInDb = await dbContext.Brackets
            .FirstOrDefaultAsync(b => b.TournamentId == tournamentCreateBracketId);

        Assert.Null(bracketInDb);
    }

    [Fact]
    public async Task GenerateBracket_ReturnsBracketAlreadyExistsError_WhenBracketAlreadyExists()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 3;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<GenerateBracketResponse>(
            MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Data.GenerateBracket.Errors);

        var errorMessage = response.Data.GenerateBracket.Errors.First().Message;
        Assert.Contains("Bracket already exists for this tournament", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentBrackets = await dbContext.Brackets
            .Where(b => b.TournamentId == tournamentCreateBracketId)
            .ToListAsync();

        Assert.Single(tournamentBrackets);
    }

    [Fact]
    public async Task GenerateBracket_ReturnsNotEnoughParticipantsError_WhenNotEnoughParticipants()
    {
        // Arrange
        var email = "bob@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 9;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<GenerateBracketResponse>(
            MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Data.GenerateBracket.Errors);

        var errorMessage = response.Data.GenerateBracket.Errors.First().Message;
        Assert.Contains("Not enough participants to create a bracket", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
    }

    [Fact]
    public async Task GenerateBracket_Succeeds_WhenEvenNumberOfParticipants()
    {
        // Arrange
        var email = "bob@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 8;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<GenerateBracketResponse>(
            MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.NotNull(response.Data.GenerateBracket.Bracket);
        Assert.Null(response.Data.GenerateBracket.Errors);

        using var dbContext = _fixture.CreateDbContext();
        var bracketInDb = await dbContext.Brackets
            .Include(b => b.Matches)
            .FirstOrDefaultAsync(b => b.TournamentId == tournamentCreateBracketId);

        Assert.NotNull(bracketInDb);
        Assert.Equal(response.Data.GenerateBracket.Bracket.Id, bracketInDb.Id);
        Assert.Single(bracketInDb.Matches);
    }

    [Fact]
    public async Task GenerateBracket_Succeeds_WhenOddNumberOfParticipants()
    {
        // Arrange
        var email = "bob@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 10;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<GenerateBracketResponse>(
            MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.NotNull(response.Data.GenerateBracket.Bracket);
        Assert.Null(response.Data.GenerateBracket.Errors);

        using var dbContext = _fixture.CreateDbContext();
        var bracketInDb = await dbContext.Brackets
            .Include(b => b.Matches)
            .FirstOrDefaultAsync(b => b.TournamentId == tournamentCreateBracketId);

        Assert.NotNull(bracketInDb);
        Assert.Equal(response.Data.GenerateBracket.Bracket.Id, bracketInDb.Id);
        Assert.Equal(2, bracketInDb.Matches.Count);
        Assert.Contains(bracketInDb.Matches, m => m.Player2Id == null);
    }

    [Fact]
    public async Task UpdateRound_ReturnsBracketNotFoundError_WhenBracketDoesNotExist()
    {
        // Arrange
        var email = "bob@example.com";
        var password = "Password123!";
        var bracketId = 999;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Data.UpdateRound.Errors);

        var errorMessage = response.Data.UpdateRound.Errors.First().Message;
        Assert.Contains("Bracket doesn't exist", errorMessage);
    }

    [Fact]
    public async Task UpdateRound_ReturnsTournamentNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var bracketId = 2;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Data.UpdateRound.Errors);

        var errorMessage = response.Data.UpdateRound.Errors.First().Message;
        Assert.Contains("User is not the owner of the tournament", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var bracketInDb = await dbContext.Brackets
            .FirstOrDefaultAsync(b => b.Id == bracketId);

        Assert.NotNull(bracketInDb);
    }

    [Fact]
    public async Task UpdateRound_ReturnsNoMatchesInRoundError_WhenNoMatchesInRound()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var bracketId = 1;
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
                bracketId = bracketId,
                roundNumber = 4
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Data.UpdateRound.Errors);

        var errorMessage = response.Data.UpdateRound.Errors.First().Message;
        Assert.Contains("No matches found in the specified round", errorMessage);
    }

    [Fact]
    public async Task UpdateRound_ReturnsNotAllMatchesPlayedError_WhenNotAllMatchesPlayed()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var bracketId = 2;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Data.UpdateRound.Errors);

        var errorMessage = response.Data.UpdateRound.Errors.First().Message;
        Assert.Contains("Not all matches in the current round have been played", errorMessage);
    }

    [Fact]
    public async Task UpdateRound_CorrectlyAdvancesWinners_WhenEvenNumberOfWinners()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var bracketId = 4;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.NotNull(response.Data.UpdateRound.Bracket);
        Assert.Null(response.Data.UpdateRound.Errors);

        using var dbContext = _fixture.CreateDbContext();
        var matchesForNextRound = await dbContext.Matches
            .Where(m => m.BracketId == bracketId && m.Round == 2)
            .ToListAsync();

        Assert.NotEmpty(matchesForNextRound);
        Assert.Equal(1, matchesForNextRound.Count);
        Assert.All(matchesForNextRound, m => Assert.NotNull(m.Player2Id));
    }

    [Fact]
    public async Task UpdateRound_CorrectlyAdvancesWinners_WhenOddNumberOfWinners()
    {
        // Arrange
        var email = "david@example.com";
        var password = "Password123!";
        var bracketId = 3;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.NotNull(response.Data.UpdateRound.Bracket);
        Assert.Null(response.Data.UpdateRound.Errors);

        using var dbContext = _fixture.CreateDbContext();
        var matchesForNextRound = await dbContext.Matches
            .Where(m => m.BracketId == bracketId && m.Round == 2)
            .ToListAsync();

        Assert.NotEmpty(matchesForNextRound);
        Assert.Equal(2, matchesForNextRound.Count);
        Assert.Contains(matchesForNextRound, m => m.Player2Id == null);
    }

    [Fact]
    public async Task UpdateRound_ReturnsNextRoundAlreadyExistsError_WhenNextRoundAlreadyExists()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var bracketId = 1;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Data.UpdateRound.Errors);

        var errorMessage = response.Data.UpdateRound.Errors.First().Message;
        Assert.Contains("Next round has already been generated", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var matchesForNextRound = await dbContext.Matches
            .Where(m => m.BracketId == bracketId && m.Round == 2)
            .ToListAsync();

        Assert.NotEmpty(matchesForNextRound);
        Assert.Equal(2, matchesForNextRound.Count);
    }

    [Fact]
    public async Task UpdateRound_ReturnsBracketAlreadyHasWinnerError_WhenBracketAlreadyHasWinner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var bracketId = 1;
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
                bracketId = bracketId,
                roundNumber = 3
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateRoundResponse>(
            MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Data.UpdateRound.Errors);

        var errorMessage = response.Data.UpdateRound.Errors.First().Message;
        Assert.Contains("Bracket already has a winner", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var matchesForNextRound = await dbContext.Matches
            .Where(m => m.BracketId == bracketId && m.Round == 4)
            .ToListAsync();

        Assert.Empty(matchesForNextRound);
    }
}
