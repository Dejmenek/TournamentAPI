using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using TournamentAPI.Configuration;

namespace TournamentAPI.EventListeners;

public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    private readonly ILogger<HttpRequestInterceptor> _logger;

    public HttpRequestInterceptor(ILogger<HttpRequestInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var apiKeyResult = await context.AuthenticateAsync(ApiKeyAuthenticationOptions.DefaultScheme);
        if (!apiKeyResult.Succeeded)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogWarning("Rejected GraphQL request from {ClientIp}: invalid or missing API key", clientIp);

            throw new GraphQLException(
                  ErrorBuilder.New()
                      .SetMessage("A valid API key is required.")
                      .SetCode("AUTH_API_KEY_INVALID")
                      .Build());
        }

        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != null)
        {
            requestBuilder.AddGlobalState("userId", userId);
        }

        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}