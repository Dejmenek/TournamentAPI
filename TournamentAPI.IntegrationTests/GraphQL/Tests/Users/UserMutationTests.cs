using Microsoft.EntityFrameworkCore;
using TournamentAPI.IntegrationTests.GraphQL.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Users;
public class UserMutationTests : BaseIntegrationTest
{
    public UserMutationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RegisterUser_CreatesNewUser()
    {
        // Arrange
        var email = "test@example.com";
        var userName = "TestUser";
        var password = "Password123!";

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteMutationAsync<RegisterResponse>(
            MutationExamples.Mutations.Users.RegisterUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password,
                    userName = userName
                }
            });

        // Assert
        Assert.NotNull(response.Data);
        Assert.True(response.Data.RegisterUser.Boolean);

        var user = await DbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);
        Assert.Equal(userName, user.UserName);
    }

    [Fact]
    public async Task RegisterUser_ReturnsEmailError_WhenEmailAlreadyExists()
    {
        // Arrange
        var email = "alice@example.com";
        var userName = "TestUser";
        var password = "Password123!";

        // Act
        using var client = CreateClient();

        var emailAlreadyExistsResponse = await client.ExecuteMutationAsync<RegisterResponse>(
            MutationExamples.Mutations.Users.RegisterUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password,
                    userName = userName
                }
            });

        // Assert
        Assert.True(emailAlreadyExistsResponse.HasErrors);
        Assert.Null(emailAlreadyExistsResponse.Data);
        var errorMessage = emailAlreadyExistsResponse.Errors!.First().Message;
        Assert.Contains("User registration failed: Email", errorMessage);
    }

    [Fact]
    public async Task RegisterUser_ReturnsPasswordError_WhenPasswordIsWeak()
    {
        // Arrange
        var email = "alice@example.com";
        var userName = "TestUser";
        var password = "weak";

        // Act
        using var client = CreateClient();

        var weakPasswordResponse = await client.ExecuteMutationAsync<RegisterResponse>(
            MutationExamples.Mutations.Users.RegisterUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password,
                    userName = userName
                }
            });

        // Assert
        Assert.True(weakPasswordResponse.HasErrors);
        Assert.Null(weakPasswordResponse.Data);
        var errorMessage = weakPasswordResponse.Errors!.First().Message;
        Assert.Contains("User registration failed: Password", errorMessage);
    }

    [Fact]
    public async Task LoginUser_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "Password123!";

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });

        // Assert
        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data);
        Assert.False(string.IsNullOrEmpty(response.Data.LoginUser.String));
    }

    [Fact]
    public async Task LoginUser_ReturnsError_WhenCredentialsAreInvalid()
    {
        // Arrange
        var email = "alice@example.com";
        var password = "WrongPassword!";

        // Act
        using var client = CreateClient();

        var response = await client.ExecuteMutationAsync<LoginResponse>(
            MutationExamples.Mutations.Users.LoginUser,
            new
            {
                input = new
                {
                    email = email,
                    password = password
                }
            });

        // Assert
        Assert.True(response.HasErrors);
        Assert.Null(response.Data);
        var errorMessage = response.Errors!.First().Message;
        Assert.Contains("Invalid email or password.", errorMessage);
    }
}
