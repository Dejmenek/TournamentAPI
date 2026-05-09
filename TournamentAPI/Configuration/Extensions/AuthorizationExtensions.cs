using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace TournamentAPI.Configuration.Extensions;

internal static class AuthorizationExtensions
{
    internal static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build())
            .AddPolicy("HealthCheckPolicy", policy =>
                policy.RequireAssertion(context =>
                {
                    var httpContext = context.Resource as HttpContext;
                    var apiKey = httpContext?.Request.Headers["X-Health-Check-Key"].ToString();
                    var expectedKey = httpContext?.RequestServices
                        .GetRequiredService<IOptions<HealthCheckApiKeyOptions>>()
                        .Value.ApiKey;

                    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(expectedKey))
                        return false;

                    var apiKeySpan = Encoding.UTF8.GetBytes(apiKey).AsSpan();
                    var expectedKeySpan = Encoding.UTF8.GetBytes(expectedKey).AsSpan();

                    return CryptographicOperations.FixedTimeEquals(apiKeySpan, expectedKeySpan);
                }));

        return services;
    }
}
