using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.IntegrationTests.GraphQL.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Tournaments;
public class TournamentMutationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public TournamentMutationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

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
}
