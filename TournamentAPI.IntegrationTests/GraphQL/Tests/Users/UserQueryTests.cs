using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Users;
public class UserQueryTests : BaseIntegrationTest
{
    public UserQueryTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMe_ReturnsCurrentUser_WhenAuthenticated()
    {
        // Arrange
        var email = "alice@example.com";
        using var client = CreateClient();

        var token = await client.ExecuteQueryAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        client.SetAuthToken(token.Data.LoginUser.String);

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMe);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Me);
        Assert.Equal(email, response.Data.Me.Email);
    }

    [Fact]
    public async Task GetMe_ReturnsOwnEmail_EvenWhenPrivate()
    {
        // Arrange
        var email = "henry@example.com";
        using var client = CreateClient();

        var token = await client.ExecuteQueryAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        client.SetAuthToken(token.Data.LoginUser.String);

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMe);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Me);
        Assert.False(response.Data.Me.IsEmailPublic);
        Assert.Equal(email, response.Data.Me.Email);
    }

    [Fact]
    public async Task GetMe_WonTournaments_ReturnsOnlyTournamentsUserWon()
    {
        // Arrange
        var email = "alice@example.com";
        using var client = CreateClient();

        var token = await client.ExecuteQueryAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        client.SetAuthToken(token.Data.LoginUser.String);

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMeWithTournamentHistory);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Me?.WonTournaments);

        var wonTournamentNames = response.Data.Me.WonTournaments.Nodes?.Select(t => t.Name).ToList();
        Assert.Equal(["Winter Championship 2024"], wonTournamentNames);
    }

    [Fact]
    public async Task GetMe_PlayedTournaments_ReturnsParticipatedTournaments_ButWonTournamentsOnlyTheWonOne()
    {
        // Arrange - alice won Tournament 3 ("Winter Championship 2024") but lost Round 1
        // of Tournament 16 ("Champions Cup"), so she should show up as having played both
        // but only won the first.
        var email = "alice@example.com";
        using var client = CreateClient();

        var token = await client.ExecuteQueryAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        client.SetAuthToken(token.Data.LoginUser.String);

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMeWithTournamentHistory);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Me?.PlayedTournaments);
        Assert.NotNull(response.Data.Me.WonTournaments);

        var playedTournamentNames = response.Data.Me.PlayedTournaments.Nodes?.Select(t => t.Name).ToList();
        Assert.Contains("Winter Championship 2024", playedTournamentNames);
        Assert.Contains("Champions Cup", playedTournamentNames);

        var wonTournamentNames = response.Data.Me.WonTournaments.Nodes?.Select(t => t.Name).ToList();
        Assert.Equal(["Winter Championship 2024"], wonTournamentNames);
        Assert.DoesNotContain("Champions Cup", wonTournamentNames);
    }

    [Fact]
    public async Task GetMe_WonTournaments_ExcludesIncompleteBrackets()
    {
        // Arrange - bob won Round 1 of Tournament 12 ("Doubles Tournament"), but no final
        // round has been generated yet, so the tournament isn't decided and shouldn't count as won.
        var email = "bob@example.com";
        using var client = CreateClient();

        var token = await client.ExecuteQueryAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        client.SetAuthToken(token.Data.LoginUser.String);

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMeWithTournamentHistory);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Me?.WonTournaments);

        var wonTournamentNames = response.Data.Me.WonTournaments.Nodes?.Select(t => t.Name).ToList();
        Assert.DoesNotContain("Doubles Tournament", wonTournamentNames);
    }

    [Fact]
    public async Task GetMe_WonMatches_IncludesMatchWinsRegardlessOfTournamentCompletion()
    {
        // Arrange - bob won Round 1 of Tournament 12 ("Doubles Tournament"), which isn't decided
        // yet (no final round generated), so it doesn't show under wonTournaments, but the match
        // win itself should still show under wonMatches.
        var email = "bob@example.com";
        using var client = CreateClient();

        var token = await client.ExecuteQueryAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        client.SetAuthToken(token.Data.LoginUser.String);

        var wonMatchInDb = await DbContext.Matches
            .AsNoTracking()
            .SingleAsync(m => m.WinnerId == 2 && m.Bracket.Tournament.Name == "Doubles Tournament");

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMeWithTournamentHistory);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Me?.WonMatches);

        var wonMatchIds = response.Data.Me.WonMatches.Nodes?.Select(m => m.Id).ToList();
        Assert.Contains(wonMatchInDb.Id, wonMatchIds);

        var wonTournamentNames = response.Data.Me.WonTournaments!.Nodes?.Select(t => t.Name).ToList();
        Assert.DoesNotContain("Doubles Tournament", wonTournamentNames);
    }

    [Fact]
    public async Task GetMe_WonMatches_ExcludesMatchesNeedingReplay()
    {
        // Arrange - flip carol's already-played win in Tournament 4 to NeedsReplay; its stale
        // WinnerId should no longer count as a win until it's replayed.
        var email = "carol@example.com";
        using var client = CreateClient();

        var match = await DbContext.Matches.FirstAsync(m => m.Bracket.Tournament.Name == "Autumn Battle" && m.WinnerId == 3);
        match.Status = MatchStatus.NeedsReplay;
        await DbContext.SaveChangesAsync();

        var token = await client.ExecuteQueryAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        client.SetAuthToken(token.Data.LoginUser.String);

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMeWithTournamentHistory);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Me?.WonMatches);

        var wonMatchIds = response.Data.Me.WonMatches.Nodes?.Select(m => m.Id).ToList();
        Assert.DoesNotContain(match.Id, wonMatchIds);
    }
}
