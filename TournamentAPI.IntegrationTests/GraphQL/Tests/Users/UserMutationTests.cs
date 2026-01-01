using Microsoft.EntityFrameworkCore;
using TournamentAPI.IntegrationTests.Extensions;
using TournamentAPI.Shared.Models;
using TournamentAPI.Users;

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
        Assert.NotNull(emailAlreadyExistsResponse.Data);
        Assert.NotNull(emailAlreadyExistsResponse.Data.RegisterUser);
        Assert.Null(emailAlreadyExistsResponse.Data.RegisterUser.Boolean);
        Assert.NotNull(emailAlreadyExistsResponse.Errors);

        var error = emailAlreadyExistsResponse.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = UserErrors.RegistrationFailed(["Email 'alice@example.com' is already taken."]);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.NotNull(error.Extensions["Errors"]);

        var errorsArray = error.GetErrorsArray();
        Assert.Contains("Email 'alice@example.com' is already taken.", errorsArray);
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
        Assert.NotNull(weakPasswordResponse.Data);
        Assert.NotNull(weakPasswordResponse.Data.RegisterUser);
        Assert.Null(weakPasswordResponse.Data.RegisterUser.Boolean);
        Assert.NotNull(weakPasswordResponse.Errors);

        var error = weakPasswordResponse.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = UserErrors.RegistrationFailed([
            "Passwords must be at least 6 characters.",
            "Passwords must have at least one non alphanumeric character.",
            "Passwords must have at least one digit ('0'-'9').",
            "Passwords must have at least one uppercase ('A'-'Z')."
        ]);
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
        Assert.NotNull(error.Extensions["Errors"]);

        var errorsArray = error.GetErrorsArray();
        Assert.Contains("Passwords must be at least 6 characters.", errorsArray);
        Assert.Contains("Passwords must have at least one non alphanumeric character.", errorsArray);
        Assert.Contains("Passwords must have at least one digit ('0'-'9').", errorsArray);
        Assert.Contains("Passwords must have at least one uppercase ('A'-'Z').", errorsArray);

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
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.LoginUser);
        Assert.Null(response.Data.LoginUser.String);
        Assert.NotNull(response.Errors);

        var error = response.Errors.First();
        Assert.NotNull(error);
        Assert.NotNull(error.Extensions);
        Assert.True(error.Extensions.ContainsKey("code"));
        Assert.NotNull(error.Message);

        var expectedError = UserErrors.InvalidCredentials();
        Assert.Equal(expectedError.Code, error.Extensions["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
    }
}
