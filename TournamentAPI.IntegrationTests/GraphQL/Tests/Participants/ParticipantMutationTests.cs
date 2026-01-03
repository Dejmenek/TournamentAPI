using Microsoft.EntityFrameworkCore;
using TournamentAPI.Shared.Models;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Participants;
public class ParticipantMutationTests : BaseIntegrationTest
{
    public ParticipantMutationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AddParticipant_ReturnsTournamentNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentId = 999;
        var participantId = 1;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentNotFound(tournamentId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
    }

    [Fact]
    public async Task AddParticipant_ReturnsTournamentNotOwnerError_WhenUserIsNotOwner()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentId = 2;
        var participantId = 1;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
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

        var tournamentParticipants = DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournamentId && tp.ParticipantId == participantId);

        Assert.Empty(tournamentParticipants);
    }

    [Fact]
    public async Task AddParticipant_ReturnsTournamentClosedError_WhenTournamentIsClosed()
    {
        // Arrange
        var email = "carol@example.com";
        var password = "Password123!";
        var tournamentId = 4;
        var participantId = 1;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.TournamentClosed(tournamentId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
    }

    [Fact]
    public async Task AddParticipant_ReturnsUserNotFoundError_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentId = 1;
        var participantId = 999;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = UserErrors.UserNotFound(participantId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var tournamentParticipants = DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournamentId && tp.ParticipantId == participantId);

        Assert.Empty(tournamentParticipants);
    }

    [Fact]
    public async Task AddParticipant_ReturnsUserAlreadyParticipantError_WhenUserIsAlreadyParticipant()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentId = 1;
        var participantId = 1;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = TournamentErrors.UserAlreadyParticipant(1, tournamentId);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.Equal(expectedError.Extensions!["TournamentId"]?.ToString(), error.Extensions["TournamentId"]?.ToString());
        Assert.Equal(expectedError.Extensions!["UserId"]?.ToString(), error.Extensions["UserId"]?.ToString());

        var tournamentParticipants = DbContext.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == tournamentId && tp.ParticipantId == participantId);

        Assert.Single(tournamentParticipants);
    }

    [Fact]
    public async Task AddParticipant_ReturnsTournamentWithBasicFields_WhenInputIsValid()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentId = 1;
        var participantId = 3;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.NotNull(response.Data.AddParticipant.Tournament);
    }

    [Fact]
    public async Task AddParticipant_ReturnsTournamentWithOwnerDetails_WhenInputIsValid()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentId = 1;
        var participantId = 4;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithOwnerDetailsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.NotNull(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Tournament.Owner);
    }

    [Fact]
    public async Task AddParticipant_ReturnsTournamentWithParticipantDetails_WhenInputIsValid()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";
        var tournamentId = 1;
        var participantId = 5;
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            Shared.MutationExamples.Mutations.Participant.AddPartiipantWithParticipantsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.NotNull(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Tournament.Participants);
        Assert.All(response.Data.AddParticipant.Tournament.Participants, p => Assert.NotNull(p.Participant));
    }
}
