using Microsoft.EntityFrameworkCore;
using TournamentAPI.IntegrationTests.GraphQL.Models;

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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Errors);

        var errorMessage = response.Data.AddParticipant.Errors.First().Message;
        Assert.Equal("Tournament doesn't exist", errorMessage);
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Errors);

        var errorMessage = response.Data.AddParticipant.Errors.First().Message;
        Assert.Equal("User is not the owner of the tournament", errorMessage);

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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Errors);

        var errorMessage = response.Data.AddParticipant.Errors.First().Message;
        Assert.Equal("Tournament is closed", errorMessage);
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Errors);

        var errorMessage = response.Data.AddParticipant.Errors.First().Message;
        Assert.Contains("The specified user was not found", errorMessage);

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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Errors);

        var errorMessage = response.Data.AddParticipant.Errors.First().Message;
        Assert.Equal("User already participates in the tournament", errorMessage);

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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Errors);
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddParticipantWithOwnerDetailsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Errors);
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
                tournamentId = tournamentId,
                userId = participantId
            }
        };

        // Act
        var response = await client.ExecuteMutationAsync<AddParticipantResponse>(
            MutationExamples.Mutations.Participant.AddPartiipantWithParticipantsReturn,
            variables);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.AddParticipant);
        Assert.Null(response.Data.AddParticipant.Errors);
        Assert.NotNull(response.Data.AddParticipant.Tournament);
        Assert.NotNull(response.Data.AddParticipant.Tournament.Participants);
        Assert.All(response.Data.AddParticipant.Tournament.Participants, p => Assert.NotNull(p.Participant));
    }
}
