using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using System.Diagnostics;
using TournamentAPI.Metrics;

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

            var operationName = context.TryGetOperation(out var operation)
                ? string.IsNullOrEmpty(operation.Name) ? "Anonymous" : operation.Name
                : "Unknown";
            var operationType = operation is not null ? operation.Kind.ToString().ToLowerInvariant() : "unknown";
            var traceId = Activity.Current?.TraceId.ToString() ?? "Unknown";
            var spanId = Activity.Current?.SpanId.ToString() ?? "Unknown";
            var userId = context.ContextData.TryGetValue("userId", out var uid) ? uid : "Anonymous";
            var entityType = context.ContextData.TryGetValue("EntityType", out var etype) ? etype : null;
            var entityId = context.ContextData.TryGetValue("EntityId", out var eid) ? eid : null;

            _graphQLMetrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, operationType, operationName);

            _logger.LogInformation(
                "GraphQL request started: {TraceId} | {SpanId} | Operation: {OperationName} | User: {UserId}",
                traceId,
                spanId,
                operationName,
                userId ?? "Anonymous");

            var errorCount = 0;
            string[] errorCodes = [];

            if (context.Result is OperationResult { Errors.Count: > 0 } result)
            {
                errorCount = result.Errors.Count;
                errorCodes = [.. result.Errors.Select(e => e.Code ?? "UNKNOWN_ERROR")];

                foreach (var errorCode in errorCodes)
                {
                    _graphQLMetrics.RecordError(operationName, errorCode);
                }
            }

            if (errorCount > 0)
            {
                _logger.LogWarning(
                    "GraphQL request completed with errors: {TraceId} | {SpanId} | Duration: {Duration}ms | Errors: {ErrorCount} | Error codes: {ErrorCodes} | Entity: {EntityType}/{EntityId}",
                    traceId,
                    spanId,
                    duration,
                    errorCount,
                    errorCodes,
                    entityType,
                    entityId);
            }
            else
            {
                _logger.LogInformation(
                    "GraphQL request completed: {TraceId} | {SpanId} | Duration: {Duration}ms | Entity: {EntityType}/{EntityId}",
                    traceId,
                    spanId,
                    duration,
                    entityType,
                    entityId);
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
