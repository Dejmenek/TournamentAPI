using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.IntegrationTests.GraphQL.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Tournaments;
public class TournamentMutationTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public TournamentMutationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateTournament_WithOwnerReturn_ReturnsOwnerDetails()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
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
                name = "Test Tournament",
                startDate = DateTime.UtcNow.AddDays(7).ToString("o"),
                status = TournamentStatus.Open.ToString().ToUpper()
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<CreateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.CreateTournamentWithOwnerReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateTournament);
        Assert.NotNull(response.Data.CreateTournament.Tournament);
        Assert.NotNull(response.Data.CreateTournament.Tournament.Owner);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentInDb = await dbContext.Tournaments
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.Id == response.Data.CreateTournament.Tournament.Id);

        Assert.NotNull(tournamentInDb);
        Assert.Equal("Test Tournament", tournamentInDb.Name);
        Assert.Equal(email, tournamentInDb.Owner.Email);
    }

    [Fact]
    public async Task CreateTournament_ReturnsNameEmptyError_WhenNameIsEmpty()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
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
                name = " ",
                startDate = DateTime.UtcNow.AddDays(7).ToString("o"),
                status = TournamentStatus.Open.ToString().ToUpper()
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<CreateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.CreateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateTournament);
        Assert.Null(response.Data.CreateTournament.Tournament);
        Assert.NotNull(response.Data.CreateTournament.Errors);

        var errorMessage = response.Data.CreateTournament.Errors.First().Message;
        Assert.Contains("Tournament name cannot be empty", errorMessage);
    }

    [Fact]
    public async Task DeleteTournament_DeletesTournamentSuccessfully()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToDeleteId = 1;
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
                tournamentId = tournamentToDeleteId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<DeleteTournamentResponse>(
            MutationExamples.Mutations.Tournaments.DeleteTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.DeleteTournament);
        Assert.True(response.Data.DeleteTournament.Boolean);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentInDb = await dbContext.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentToDeleteId);

        Assert.Null(tournamentInDb);
    }

    [Fact]
    public async Task DeleteTournament_ReturnsNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToDeleteId = 999;
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
                tournamentId = tournamentToDeleteId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<DeleteTournamentResponse>(
            MutationExamples.Mutations.Tournaments.DeleteTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.DeleteTournament);
        Assert.NotNull(response.Data.DeleteTournament.Errors);

        var errorMessage = response.Data.DeleteTournament.Errors.First().Message;
        Assert.Contains("Tournament doesn't exist", errorMessage);
    }

    [Fact]
    public async Task DeleteTournament_ReturnsNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToDeleteId = 2;
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
                tournamentId = tournamentToDeleteId
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<DeleteTournamentResponse>(
            MutationExamples.Mutations.Tournaments.DeleteTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.DeleteTournament);
        Assert.NotNull(response.Data.DeleteTournament.Errors);

        var errorMessage = response.Data.DeleteTournament.Errors.First().Message;
        Assert.Contains("User is not the owner of the tournament", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentInDb = await dbContext.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentToDeleteId);

        Assert.NotNull(tournamentInDb);
    }

    [Fact]
    public async Task UpdateTournament_ReturnsNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToUpdateId = 999;
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
                tournamentId = tournamentToUpdateId,
                name = "Updated Tournament Name",
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = DateTime.UtcNow.AddDays(10).ToString("o")
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.Null(response.Data.UpdateTournament.Tournament);
        Assert.NotNull(response.Data.UpdateTournament.Errors);

        var errorMessage = response.Data.UpdateTournament.Errors.First().Message;
        Assert.Contains("Tournament doesn't exist", errorMessage);
    }

    [Fact]
    public async Task UpdateTournament_ReturnsNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToUpdateId = 2;
        var updatedTournamentName = "Updated Tournament Name";
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName,
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = DateTime.UtcNow.AddDays(10).ToString("o")
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.Null(response.Data.UpdateTournament.Tournament);
        Assert.NotNull(response.Data.UpdateTournament.Errors);

        var errorMessage = response.Data.UpdateTournament.Errors.First().Message;
        Assert.Contains("User is not the owner of the tournament", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentInDb = await dbContext.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentToUpdateId);

        Assert.NotNull(tournamentInDb);
        Assert.NotEqual(updatedTournamentName, tournamentInDb.Name);
    }

    [Fact]
    public async Task UpdateTournament_ReturnsNameEmptyError_WhenNameIsEmpty()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToUpdateId = 1;
        var updatedTournamentName = " ";
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName,
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = DateTime.UtcNow.AddDays(10).ToString("o")
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.Null(response.Data.UpdateTournament.Tournament);
        Assert.NotNull(response.Data.UpdateTournament.Errors);

        var errorMessage = response.Data.UpdateTournament.Errors.First().Message;
        Assert.Contains("Tournament name cannot be empty", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentInDb = await dbContext.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentToUpdateId);

        Assert.NotNull(tournamentInDb);
        Assert.NotEqual(updatedTournamentName, tournamentInDb.Name);
    }

    [Fact]
    public async Task UpdateTournament_UpdatesTournamentSuccessfully()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToUpdateId = 1;
        var updatedTournamentName = "Updated Tournament Name";
        var updatedDate = DateTime.UtcNow.AddDays(10);
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName,
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = updatedDate.ToString("o")
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.NotNull(response.Data.UpdateTournament.Tournament);
        Assert.Equal(updatedTournamentName, response.Data.UpdateTournament.Tournament.Name);
        Assert.Equal(TournamentStatus.Closed.ToString().ToUpper(), response.Data.UpdateTournament.Tournament.Status);
        Assert.Null(response.Data.UpdateTournament.Errors);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentInDb = await dbContext.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentToUpdateId);

        Assert.NotNull(tournamentInDb);
        Assert.Equal(updatedTournamentName, tournamentInDb.Name);
        Assert.Equal(TournamentStatus.Closed, tournamentInDb.Status);
    }

    [Fact]
    public async Task UpdateTournament_PartiallyUpdatesTournamentSuccessfully()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToUpdateId = 1;
        var updatedTournamentName = "Updated Tournament Name";
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.NotNull(response.Data.UpdateTournament.Tournament);
        Assert.Equal(updatedTournamentName, response.Data.UpdateTournament.Tournament.Name);
        Assert.Null(response.Data.UpdateTournament.Errors);

        using var dbContext = _fixture.CreateDbContext();
        var tournamentInDb = await dbContext.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentToUpdateId);

        Assert.NotNull(tournamentInDb);
        Assert.Equal(updatedTournamentName, tournamentInDb.Name);
    }

    [Fact]
    public async Task UpdateTournament_WithOwnerReturn_ReturnsOwnerDetails()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToUpdateId = 1;
        var updatedTournamentName = "Updated Tournament Name";
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithOwnerReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.NotNull(response.Data.UpdateTournament.Tournament);
        Assert.NotNull(response.Data.UpdateTournament.Tournament.Owner);
        Assert.Equal(updatedTournamentName, response.Data.UpdateTournament.Tournament.Name);
    }

    [Fact]
    public async Task JoinTournament_ReturnsTournamentNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToJoinId = 999;
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.Null(response.Data.JoinTournament.Boolean);
        Assert.NotNull(response.Data.JoinTournament.Errors);

        var errorMessage = response.Data.JoinTournament.Errors.First().Message;
        Assert.Contains("Tournament doesn't exist", errorMessage);
    }

    [Fact]
    public async Task JoinTournament_ReturnsClosedError_WhenTournamentIsClosed()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToJoinId = 4;
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.Null(response.Data.JoinTournament.Boolean);
        Assert.NotNull(response.Data.JoinTournament.Errors);

        var errorMessage = response.Data.JoinTournament.Errors.First().Message;
        Assert.Contains("Tournament is closed", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var participantInDb = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(tp => tp.TournamentId == tournamentToJoinId && tp.Participant.Email == email);

        Assert.Null(participantInDb);
    }

    [Fact]
    public async Task JoinTournament_ReturnsJoinFailedError_WhenUserAlreadyParticipates()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToJoinId = 1;
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.Null(response.Data.JoinTournament.Boolean);
        Assert.NotNull(response.Data.JoinTournament.Errors);

        var errorMessage = response.Data.JoinTournament.Errors.First().Message;
        Assert.Contains("User already participates in the tournament", errorMessage);

        using var dbContext = _fixture.CreateDbContext();
        var participantInDb = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(tp => tp.TournamentId == tournamentToJoinId && tp.Participant.Email == email);

        Assert.NotNull(participantInDb);
    }

    [Fact]
    public async Task JoinTournament_JoinsTournamentSuccessfully()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToJoinId = 2;
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await _fixture.Client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.True(response.Data.JoinTournament.Boolean);
        Assert.Null(response.Data.JoinTournament.Errors);

        using var dbContext = _fixture.CreateDbContext();
        var participantInDb = await dbContext.TournamentParticipants
            .FirstOrDefaultAsync(tp => tp.TournamentId == tournamentToJoinId && tp.Participant.Email == email);

        Assert.NotNull(participantInDb);
    }
}
