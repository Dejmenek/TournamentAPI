using HotChocolate.Execution;

namespace TournamentAPI.EventListeners;

public sealed class UnhandledExceptionErrorFilter : IErrorFilter
{
    private readonly ILogger<UnhandledExceptionErrorFilter> _logger;

    public UnhandledExceptionErrorFilter(ILogger<UnhandledExceptionErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is null)
        {
            return error;
        }

        _logger.LogError(
            error.Exception,
            "Unhandled exception in GraphQL request: {Message}",
            error.Exception.Message);

        return error.Code is null
            ? ErrorBuilder.FromError(error).SetCode("UNHANDLED_EXCEPTION").Build()
            : error;
    }
}
