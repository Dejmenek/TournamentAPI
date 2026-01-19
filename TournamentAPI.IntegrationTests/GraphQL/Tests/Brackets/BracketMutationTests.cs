using Microsoft.EntityFrameworkCore;
using TournamentAPI.Brackets;
using TournamentAPI.Shared.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Brackets;
public class BracketMutationTests : BaseIntegrationTest
{
    public BracketMutationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GenerateBracket_ReturnsTournamentNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 9999;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotFound(tournamentCreateBracketId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
    }

    [Fact]
    public async Task GenerateBracket_ReturnsTournamentNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 2;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotOwner(1, tournamentCreateBracketId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var bracketInDb = await DbContext.Brackets
            .AsNoTracking()
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.BracketGenerationNotAllowed(tournamentCreateBracketId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());

        var bracketInDb = await DbContext.Brackets
            .AsNoTracking()
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
        var bracketId = 1;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.BracketAlreadyExistsForTournament(tournamentCreateBracketId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());

        var tournamentBrackets = await DbContext.Brackets
            .AsNoTracking()
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
        var participantsCount = 0;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.Null(response.Data.GenerateBracket.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.NotEnoughParticipants(participantsCount);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["ParticipantCount"]?.ToString(), error.Extensions["ParticipantCount"]?.ToString());

        var bracketInDb = await DbContext.Brackets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TournamentId == tournamentCreateBracketId);

        Assert.Null(bracketInDb);
    }

    [Fact]
    public async Task GenerateBracket_Succeeds_WhenEvenNumberOfParticipants()
    {
        // Arrange
        var email = "bob@example.com";
        var password = "Password123!";
        var tournamentCreateBracketId = 8;
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.NotNull(response.Data.GenerateBracket.Bracket);

        var bracketInDb = await DbContext.Brackets
            .Include(b => b.Matches)
            .AsNoTracking()
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
                tournamentId = tournamentCreateBracketId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<GenerateBracketResponse>(
            Shared.MutationExamples.Mutations.Bracket.GenerateBracket,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.GenerateBracket);
        Assert.NotNull(response.Data.GenerateBracket.Bracket);

        var bracketInDb = await DbContext.Brackets
            .Include(b => b.Matches)
            .AsNoTracking()
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.BracketNotFound(bracketId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["BracketId"]?.ToString(), error.Extensions["BracketId"]?.ToString());
    }

    [Fact]
    public async Task UpdateRound_ReturnsTournamentNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var bracketId = 2;
        var tournamentId = 4;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotOwner(1, tournamentId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var bracketInDb = await DbContext.Brackets
            .AsNoTracking()
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
        var roundNumber = 4;
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
                bracketId = bracketId,
                roundNumber = roundNumber
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.NoMatchesInRound(roundNumber);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["RoundNumber"]?.ToString(), error.Extensions["RoundNumber"]?.ToString());
    }

    [Fact]
    public async Task UpdateRound_ReturnsNotAllMatchesPlayedError_WhenNotAllMatchesPlayed()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var bracketId = 2;
        var roundNumber = 1;
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
                bracketId = bracketId,
                roundNumber = roundNumber
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.NotAllMatchesPlayed(roundNumber);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["RoundNumber"]?.ToString(), error.Extensions["RoundNumber"]?.ToString());
    }

    [Fact]
    public async Task UpdateRound_CorrectlyAdvancesWinners_WhenEvenNumberOfWinners()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var bracketId = 4;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.NotNull(response.Data.UpdateRound.Bracket);

        var matchesForNextRound = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.BracketId == bracketId && m.Round == 2)
            .ToListAsync();

        Assert.NotEmpty(matchesForNextRound);
        Assert.Single(matchesForNextRound);
        Assert.All(matchesForNextRound, m => Assert.NotNull(m.Player2Id));
    }

    [Fact]
    public async Task UpdateRound_CorrectlyAdvancesWinners_WhenOddNumberOfWinners()
    {
        // Arrange
        var email = "david@example.com";
        var password = "Password123!";
        var bracketId = 3;
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.NotNull(response.Data.UpdateRound.Bracket);

        var matchesForNextRound = await DbContext.Matches
            .AsNoTracking()
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
                bracketId = bracketId,
                roundNumber = 1
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.NextRoundAlreadyGenerated(bracketId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["BracketId"]?.ToString(), error.Extensions["BracketId"]?.ToString());

        var matchesForNextRound = await DbContext.Matches
            .AsNoTracking()
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
                bracketId = bracketId,
                roundNumber = 3
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateRoundResponse>(
            Shared.MutationExamples.Mutations.Bracket.UpdateRound,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateRound);
        Assert.Null(response.Data.UpdateRound.Bracket);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = BracketErrors.BracketAlreadyHasWinner(bracketId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["BracketId"]?.ToString(), error.Extensions["BracketId"]?.ToString());

        var matchesForNextRound = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.BracketId == bracketId && m.Round == 4)
            .ToListAsync();

        Assert.Empty(matchesForNextRound);
    }
}
