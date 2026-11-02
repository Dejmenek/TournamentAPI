using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Payments;

public class PayoutAccountStatusQueryTests : BaseIntegrationTest
{
    public PayoutAccountStatusQueryTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Me_ReturnsNotStarted_WhenUserHasNoPayoutAccount()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync("alice@example.com");

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(Shared.QueryExamples.Queries.Users.GetMePayoutAccountStatus);

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal("NOT_STARTED", response.Data?.Me?.PayoutAccountStatus);
    }

    [Fact]
    public async Task Me_ReturnsOwnPayoutAccountStatus()
    {
        // Arrange
        await SetPayoutStatusAsync("alice@example.com", PayoutAccountStatus.Enabled);
        using var client = await CreateClientLoggedInAsAsync("alice@example.com");

        // Act
        var response = await client.ExecuteQueryAsync<MeResponse>(Shared.QueryExamples.Queries.Users.GetMePayoutAccountStatus);

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal("ENABLED", response.Data?.Me?.PayoutAccountStatus);
    }

    [Fact]
    public async Task TournamentOwner_HidesPayoutAccountStatus_FromOtherUsers()
    {
        // Arrange
        var bob = await SetPayoutStatusAsync("bob@example.com", PayoutAccountStatus.Enabled);
        var summerCup = await DbContext.Tournaments.AsNoTracking().FirstAsync(t => t.OwnerId == bob.Id);
        using var client = await CreateClientLoggedInAsAsync("alice@example.com");

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithOwnerPayoutAccountStatus,
            new { id = summerCup.Id });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal(bob.Id, response.Data?.TournamentById?.Owner?.Id);
        Assert.Null(response.Data?.TournamentById?.Owner?.PayoutAccountStatus);
    }

    [Fact]
    public async Task TournamentOwner_HidesPayoutAccountStatus_FromAnonymousViewers()
    {
        // Arrange
        var bob = await SetPayoutStatusAsync("bob@example.com", PayoutAccountStatus.Enabled);
        var summerCup = await DbContext.Tournaments.AsNoTracking().FirstAsync(t => t.OwnerId == bob.Id);
        using var client = CreateClient();

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithOwnerPayoutAccountStatus,
            new { id = summerCup.Id });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Null(response.Data?.TournamentById?.Owner?.PayoutAccountStatus);
    }

    [Fact]
    public async Task TournamentOwner_ShowsPayoutAccountStatus_ToTheOwner()
    {
        // Arrange
        var bob = await SetPayoutStatusAsync("bob@example.com", PayoutAccountStatus.Restricted);
        var summerCup = await DbContext.Tournaments.AsNoTracking().FirstAsync(t => t.OwnerId == bob.Id);
        using var client = await CreateClientLoggedInAsAsync("bob@example.com");

        // Act
        var response = await client.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithOwnerPayoutAccountStatus,
            new { id = summerCup.Id });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal("RESTRICTED", response.Data?.TournamentById?.Owner?.PayoutAccountStatus);
    }

    private async Task<ApplicationUser> SetPayoutStatusAsync(string email, PayoutAccountStatus status)
    {
        var user = await DbContext.Users.FirstAsync(u => u.Email == email);
        user.StripeAccountId = $"acct_test_{user.UserName}";
        user.PayoutAccountStatus = status;
        await DbContext.SaveChangesAsync();
        return user;
    }

    private async Task<TestClient> CreateClientLoggedInAsAsync(string email)
    {
        var client = CreateClient();
        var token = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email, password = "Password123!" } });
        client.SetAuthToken(token.Data!.LoginUser!.String!);
        return client;
    }
}
