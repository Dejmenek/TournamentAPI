using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Extensions;
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
            Shared.MutationExamples.Mutations.Users.RegisterUser,
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
            Shared.MutationExamples.Mutations.Users.RegisterUser,
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
            Shared.MutationExamples.Mutations.Users.RegisterUser,
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
            Shared.MutationExamples.Mutations.Users.LoginUser,
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
            Shared.MutationExamples.Mutations.Users.LoginUser,
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

    [Fact]
    public async Task LoginUser_SetsRefreshTokenCookie()
    {
        using var client = CreateClient();

        await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email = "alice@example.com", password = "Password123!" } });

        var cookie = client.GetRefreshTokenCookie();
        Assert.NotNull(cookie);
        Assert.NotEmpty(cookie);
    }

    [Fact]
    public async Task RefreshToken_ReturnsNewAccessToken_WhenCookieIsValid()
    {
        using var client = CreateClient();

        await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email = "alice@example.com", password = "Password123!" } });

        var refreshToken = client.GetRefreshTokenCookie();
        Assert.NotNull(refreshToken);
        client.SetRefreshTokenCookie(refreshToken);

        var response = await client.ExecuteMutationAsync<RefreshTokenResponse>(
            Shared.MutationExamples.Mutations.Users.RefreshToken,
            new { });

        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.RefreshToken?.String);
    }

    [Fact]
    public async Task RefreshToken_RotatesToken()
    {
        using var client = CreateClient();

        await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email = "alice@example.com", password = "Password123!" } });

        var originalToken = client.GetRefreshTokenCookie();
        Assert.NotNull(originalToken);
        client.SetRefreshTokenCookie(originalToken);

        await client.ExecuteMutationAsync<RefreshTokenResponse>(
            Shared.MutationExamples.Mutations.Users.RefreshToken,
            new { });

        var rotatedToken = client.GetRefreshTokenCookie();
        Assert.NotNull(rotatedToken);
        Assert.NotEqual(originalToken, rotatedToken);
    }

    [Fact]
    public async Task RefreshToken_ReturnsInvalidError_WhenCookieIsMissing()
    {
        using var client = CreateClient();

        var response = await client.ExecuteMutationAsync<RefreshTokenResponse>(
            Shared.MutationExamples.Mutations.Users.RefreshToken,
            new { });

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = UserErrors.RefreshTokenInvalid();
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
    }

    [Fact]
    public async Task RefreshToken_ReturnsExpiredError_WhenTokenIsExpired()
    {
        var alice = await DbContext.Users.FirstAsync(u => u.Email == "alice@example.com");
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = alice.Id,
            Token = "expired-token-value",
            ExpiryDateUtc = DateTime.UtcNow.AddDays(-1)
        };
        DbContext.RefreshTokens.Add(expiredToken);
        await DbContext.SaveChangesAsync();

        using var client = CreateClient();
        client.SetRefreshTokenCookie(expiredToken.Token);

        var response = await client.ExecuteMutationAsync<RefreshTokenResponse>(
            Shared.MutationExamples.Mutations.Users.RefreshToken,
            new { });

        Assert.True(response.HasErrors);
        var error = response.Errors!.First();
        var expectedError = UserErrors.RefreshTokenExpired();
        Assert.Equal(expectedError.Code, error.Extensions!["code"]?.ToString());
        Assert.Equal(expectedError.Message, error.Message);
    }

    [Fact]
    public async Task LoginUser_RemovesOldRefreshTokens_OnReLogin()
    {
        using var client = CreateClient();
        var loginVars = new { input = new { email = "alice@example.com", password = "Password123!" } };

        await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser, loginVars);

        await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser, loginVars);

        var alice = await DbContext.Users.FirstAsync(u => u.Email == "alice@example.com");
        var tokenCount = await DbContext.RefreshTokens.CountAsync(r => r.UserId == alice.Id);
        Assert.Equal(1, tokenCount);
    }
}
