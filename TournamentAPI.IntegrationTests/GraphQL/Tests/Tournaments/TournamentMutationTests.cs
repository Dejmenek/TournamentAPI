using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Tournaments;
public class TournamentMutationTests : BaseIntegrationTest
{
    public TournamentMutationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateTournament_WithOwnerReturn_ReturnsOwnerDetails()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
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
                name = "Test Tournament",
                startDate = DateTime.UtcNow.AddDays(7).ToString("o"),
                status = TournamentStatus.Open.ToString().ToUpper()
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<CreateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.CreateTournamentWithOwnerReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateTournament);
        Assert.NotNull(response.Data.CreateTournament.Tournament);
        Assert.NotNull(response.Data.CreateTournament.Tournament.Owner);

        var tournamentInDb = await DbContext.Tournaments
            .Include(t => t.Owner)
            .AsNoTracking()
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
                name = " ",
                startDate = DateTime.UtcNow.AddDays(7).ToString("o"),
                status = TournamentStatus.Open.ToString().ToUpper()
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<CreateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.CreateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.CreateTournament);
        Assert.Null(response.Data.CreateTournament.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNameEmpty();
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
    }

    [Fact]
    public async Task DeleteTournament_DeletesTournamentSuccessfully()
    {
        // Arrange
        var email = "bob@example.com";
        var password = "Password123!";
        var tournamentToDeleteId = 9;
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
                tournamentId = tournamentToDeleteId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<DeleteTournamentResponse>(
            MutationExamples.Mutations.Tournaments.DeleteTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.DeleteTournament);
        Assert.True(response.Data.DeleteTournament.Boolean);

        var tournamentInDb = await DbContext.Tournaments
            .AsNoTracking()
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
                tournamentId = tournamentToDeleteId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<DeleteTournamentResponse>(
            MutationExamples.Mutations.Tournaments.DeleteTournament,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.DeleteTournament);
        Assert.Null(response.Data.DeleteTournament.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotFound(tournamentToDeleteId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
    }

    [Fact]
    public async Task DeleteTournament_ReturnsNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToDeleteId = 2;
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
                tournamentId = tournamentToDeleteId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<DeleteTournamentResponse>(
            MutationExamples.Mutations.Tournaments.DeleteTournament,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.DeleteTournament);
        Assert.Null(response.Data.DeleteTournament.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotOwner(1, tournamentToDeleteId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var tournamentInDb = await DbContext.Tournaments
            .AsNoTracking()
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
                tournamentId = tournamentToUpdateId,
                name = "Updated Tournament Name",
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = DateTime.UtcNow.AddDays(10).ToString("o")
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.Null(response.Data.UpdateTournament.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotFound(tournamentToUpdateId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
    }

    [Fact]
    public async Task UpdateTournament_ReturnsNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToUpdateId = 2;
        var updatedTournamentName = "Updated Tournament Name";
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName,
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = DateTime.UtcNow.AddDays(10).ToString("o")
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.Null(response.Data.UpdateTournament.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotOwner(1, tournamentToUpdateId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var tournamentInDb = await DbContext.Tournaments
            .AsNoTracking()
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName,
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = DateTime.UtcNow.AddDays(10).ToString("o")
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.Null(response.Data.UpdateTournament.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNameEmpty();
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);

        var tournamentInDb = await DbContext.Tournaments
            .AsNoTracking()
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName,
                status = TournamentStatus.Closed.ToString().ToUpper(),
                startDate = updatedDate.ToString("o")
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.NotNull(response.Data.UpdateTournament.Tournament);
        Assert.Equal(updatedTournamentName, response.Data.UpdateTournament.Tournament.Name);
        Assert.Equal(TournamentStatus.Closed.ToString().ToUpper(), response.Data.UpdateTournament.Tournament.Status);

        var tournamentInDb = await DbContext.Tournaments
            .AsNoTracking()
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateTournamentResponse>(
            MutationExamples.Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.UpdateTournament);
        Assert.NotNull(response.Data.UpdateTournament.Tournament);
        Assert.Equal(updatedTournamentName, response.Data.UpdateTournament.Tournament.Name);

        var tournamentInDb = await DbContext.Tournaments
            .AsNoTracking()
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
                tournamentId = tournamentToUpdateId,
                name = updatedTournamentName
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<UpdateTournamentResponse>(
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.Null(response.Data.JoinTournament.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotFound(tournamentToJoinId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
    }

    [Fact]
    public async Task JoinTournament_ReturnsClosedError_WhenTournamentIsClosed()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentToJoinId = 4;
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.Null(response.Data.JoinTournament.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentClosed(tournamentToJoinId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());

        var participantInDb = await DbContext.TournamentParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.TournamentId == tournamentToJoinId && tp.Participant.Email == email);

        Assert.Null(participantInDb);
    }

    [Fact]
    public async Task JoinTournament_ReturnsJoinFailedError_WhenUserAlreadyParticipates()
    {
        // Arrange
        var email = "bob@example.com";
        var password = "Password123!";
        var tournamentToJoinId = 2;
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.Null(response.Data.JoinTournament.Boolean);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.UserAlreadyParticipant(2, tournamentToJoinId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var participantInDb = await DbContext.TournamentParticipants
            .AsNoTracking()
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
                tournamentId = tournamentToJoinId,
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<JoinTournamentResponse>(
            MutationExamples.Mutations.Tournaments.JoinTournament,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.JoinTournament);
        Assert.True(response.Data.JoinTournament.Boolean);

        var participantInDb = await DbContext.TournamentParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.TournamentId == tournamentToJoinId && tp.Participant.Email == email);

        Assert.NotNull(participantInDb);
    }
}
