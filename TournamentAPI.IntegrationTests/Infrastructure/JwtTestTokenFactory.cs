using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TournamentAPI.Configuration;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public static class JwtTestTokenFactory
{
    public static string CreateExpiredToken(JwtOptions options, int userId, string userName, string email)
    {
        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            CreateClaims(userId, userName, email),
            expires: DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: CreateSigningCredentials(options.Key));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateTokenWithWrongSignature(JwtOptions options, int userId, string userName, string email)
    {
        var wrongKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            CreateClaims(userId, userName, email),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: CreateSigningCredentials(wrongKey));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateTokenMissingNameIdentifierClaim(JwtOptions options, int userId, string userName, string email)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email)
        };

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: CreateSigningCredentials(options.Key));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateMalformedTokenString() => "not.a.jwt";

    private static Claim[] CreateClaims(int userId, string userName, string email)
    {
        return
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ];
    }

    private static SigningCredentials CreateSigningCredentials(string key)
    {
        return new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
    }
}
