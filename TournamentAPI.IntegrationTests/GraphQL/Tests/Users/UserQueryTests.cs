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
}
