using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class GraphQLMetrics
{
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _errorsTotal;

    public GraphQLMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.GraphQLMeterName);
        _requestDuration = meter.CreateHistogram<double>(
            "graphql.server.request.duration",
            unit: "s",
            description: "Duration of GraphQL requests");
        _errorsTotal = meter.CreateCounter<long>(
            "graphql.server.errors_total",
            description: "Number of GraphQL errors returned to clients");
    }

    public void RecordRequestDuration(double durationSeconds, string operationType, string operationName) =>
        _requestDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("graphql.operation.type", operationType),
            new KeyValuePair<string, object?>("graphql.operation.name", operationName));

    public void RecordError(string operationName, string errorCode) =>
        _errorsTotal.Add(1,
            new KeyValuePair<string, object?>("graphql.operation.name", operationName),
            new KeyValuePair<string, object?>("graphql.error_code", errorCode));
}
