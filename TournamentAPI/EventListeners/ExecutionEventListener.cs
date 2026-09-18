using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using System.Diagnostics;

namespace TournamentAPI.EventListeners;

public sealed class ExecutionEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<ExecutionEventListener> _logger;
    private readonly GraphQLMetrics _graphQLMetrics;

    public ExecutionEventListener(ILogger<ExecutionEventListener> logger, GraphQLMetrics graphQLMetrics)
    {
        _logger = logger;
        _graphQLMetrics = graphQLMetrics;
    }

    public override IDisposable ExecuteRequest(RequestContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        return new RequestScope(() =>
        {
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;

            var operationType = context.TryGetOperation(out var operation) ? operation.Kind.ToString() : "Unknown";
            var requestId = context.ContextData.TryGetValue("requestId", out var reqId) ? reqId : "Unknown";
            var userId = context.ContextData.TryGetValue("userId", out var uid) ? uid : "Anonymous";

            _graphQLMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, operationType, operationName);

            _logger.LogInformation(
                "GraphQL request started: {RequestId} | Operation: {OperationType} | User: {UserId}",
                requestId,
                operationType,
                userId ?? "Anonymous");

            var errorCount = 0;
            string[] errorCodes = [];

            if (context.Result != null)
            {
                var resultType = context.Result.GetType();
                var errorsProperty = resultType.GetProperty("Errors");

                if (errorsProperty != null)
                {
                    if (errorsProperty.GetValue(context.Result) is IEnumerable<IError> errors)
                    {
                    _graphQLMetrics.RecordError(operationName, errorCode);
                        errorCount = errorList.Count;
                        errorCodes = [.. errorList.Select(e => e.Code ?? "UNKNOWN_ERROR")];
                    }
                }
            }

            if (errorCount > 0)
            {
                _logger.LogWarning(
                    "GraphQL request completed with errors: {RequestId} | Duration: {Duration}ms | Errors: {ErrorCount} | Error codes: {ErrorCodes}",
                    requestId,
                    duration,
                    errorCount,
                    errorCodes);
            }
            else
            {
                _logger.LogInformation(
                    "GraphQL request completed: {RequestId} | Duration: {Duration}ms",
                    requestId,
                    duration);
            }
        });
    }
}

public sealed class RequestScope : IDisposable
{
    private readonly Action _onDispose;
    public RequestScope(Action onDispose) => _onDispose = onDispose;
    public void Dispose() => _onDispose();
}
