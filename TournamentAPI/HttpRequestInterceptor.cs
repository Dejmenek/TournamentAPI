using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace TournamentAPI;

public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    private readonly IPolicyEvaluator _policyEvaluator;
    private readonly IAuthorizationPolicyProvider _policyProvider;

    public HttpRequestInterceptor(
        IPolicyEvaluator policyEvaluator,
        IAuthorizationPolicyProvider policyProvider)
    {
        _policyEvaluator = policyEvaluator;
        _policyProvider = policyProvider;
    }

    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var policy = await _policyProvider.GetDefaultPolicyAsync();
        var authenticateResult = await _policyEvaluator.AuthenticateAsync(policy, context);

        if (authenticateResult.Succeeded && authenticateResult.Principal != null)
        {
            context.User = authenticateResult.Principal;
        }

        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}

