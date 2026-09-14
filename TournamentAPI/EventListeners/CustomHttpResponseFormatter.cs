using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Execution;
using System.Net;

namespace TournamentAPI.EventListeners;

public sealed class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    protected override HttpStatusCode OnDetermineStatusCode(
        OperationResult result, FormatInfo format, HttpStatusCode? proposedStatusCode)
    {
        if (result.Errors?.Any(e => e.Code == "AUTH_API_KEY_INVALID") is true)
        {
            return HttpStatusCode.Unauthorized;
        }

        return base.OnDetermineStatusCode(result, format, proposedStatusCode);
    }
}
