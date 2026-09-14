using Microsoft.AspNetCore.Authentication;

namespace TournamentAPI.Configuration;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
}
