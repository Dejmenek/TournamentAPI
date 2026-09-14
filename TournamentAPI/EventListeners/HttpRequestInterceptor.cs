using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using TournamentAPI.Configuration;

namespace TournamentAPI.EventListeners;

public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var apiKeyResult = await context.AuthenticateAsync(ApiKeyAuthenticationOptions.DefaultScheme);
        if (!apiKeyResult.Succeeded)
        {
            throw new GraphQLException(
                  ErrorBuilder.New()
                      .SetMessage("A valid API key is required.")
                      .SetCode("AUTH_API_KEY_INVALID")
                      .Build());
        }

        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var requestId = Guid.NewGuid().ToString();

        if (userId != null)
        {
            requestBuilder.AddGlobalState("userId", userId);
        }

        requestBuilder.AddGlobalState("requestId", requestId);

        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}