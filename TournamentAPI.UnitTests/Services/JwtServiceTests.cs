using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TournamentAPI.Configuration;
using TournamentAPI.Data.Models;
using TournamentAPI.Services;

namespace TournamentAPI.UnitTests.Services;

public class JwtServiceTests
{
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "test-secret-key-that-is-at-least-32-bytes-long!"
        });

        _sut = new JwtService(jwtOptions);
    }

    [Fact]
    public void CreateToken_ReturnsNonEmptyString()
    {
        var user = CreateTestUser();

        string token = _sut.CreateToken(user);

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void CreateToken_ReturnsValidJwt()
    {
        var user = CreateTestUser();

        string token = _sut.CreateToken(user);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void CreateToken_ContainsCorrectIssuerAndAudience()
    {
        var user = CreateTestUser();

        string token = _sut.CreateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }

    [Fact]
    public void CreateToken_ContainsUserIdClaim()
    {
        var user = CreateTestUser(id: 42);

        string token = _sut.CreateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal("42", subClaim.Value);
    }

    [Fact]
    public void CreateToken_ContainsUserNameClaim()
    {
        var user = CreateTestUser(userName: "johndoe");

        string token = _sut.CreateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("johndoe", nameClaim.Value);
    }

    [Fact]
    public void CreateToken_ContainsEmailClaim()
    {
        var user = CreateTestUser(email: "john@example.com");

        string token = _sut.CreateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal("john@example.com", emailClaim.Value);
    }

    [Fact]
    public void CreateToken_ContainsNameIdentifierClaim()
    {
        var user = CreateTestUser(id: 42);

        string token = _sut.CreateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var nameIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(nameIdClaim);
        Assert.Equal("42", nameIdClaim.Value);
    }

    [Fact]
    public void HashRefreshToken_ReturnsNonEmptyString()
    {
        var hash = _sut.HashRefreshToken("some-token");

        Assert.False(string.IsNullOrEmpty(hash));
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic()
    {
        var token = "some-token";

        var hash1 = _sut.HashRefreshToken(token);
        var hash2 = _sut.HashRefreshToken(token);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashRefreshToken_ReturnsDifferentHashesForDifferentTokens()
    {
        var hash1 = _sut.HashRefreshToken("token-one");
        var hash2 = _sut.HashRefreshToken("token-two");

        Assert.NotEqual(hash1, hash2);
    }

    private static ApplicationUser CreateTestUser(int id = 1, string userName = "testuser", string email = "test@example.com")
        => new()
        {
            Id = id,
            UserName = userName,
            Email = email
        };
}
