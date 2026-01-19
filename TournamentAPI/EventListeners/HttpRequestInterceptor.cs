using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using System.Security.Claims;

namespace TournamentAPI.EventListeners;

public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var requestId = Guid.NewGuid().ToString();

        if (userId != null)
        {
            requestBuilder.AddGlobalState("userId", userId);
        }

        requestBuilder.AddGlobalState("requestId", requestId);

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}