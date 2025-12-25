using TournamentAPI.IntegrationTests.GraphQL.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Users;
public class UserQueryTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public UserQueryTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetMe_ReturnsCurrentUser_WhenAuthenticated()
    {
        // Arrange
        var email = "alice@example.com";
        var token = await _fixture.Client.ExecuteQueryAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = "Password123!"
                }
            });
        _fixture.Client.SetAuthToken(token.Data.LoginUser.String);

        // Act
        var response = await _fixture.Client.ExecuteQueryAsync<MeResponse>(
            QueryExamples.Queries.Users.GetMe);

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Me);
        Assert.Equal(email, response.Data.Me.Email);
    }
}
