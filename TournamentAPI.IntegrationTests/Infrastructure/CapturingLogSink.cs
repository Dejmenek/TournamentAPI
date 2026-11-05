using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed record CapturedLog(LogLevel Level, string Category, string Message);

/// <summary>
/// Collects what the application logs through its real Serilog pipeline, so tests can assert on log output.
/// </summary>
public sealed class CapturingLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<CapturedLog> _logs = new();

    public IReadOnlyList<CapturedLog> Logs => _logs.ToArray();

    public void Emit(LogEvent logEvent)
    {
        var category = logEvent.Properties.TryGetValue("SourceContext", out var source)
            ? source.ToString().Trim('"')
            : string.Empty;

        _logs.Enqueue(new CapturedLog(ToLogLevel(logEvent.Level), category, logEvent.RenderMessage()));
    }

    public void Reset() => _logs.Clear();

    private static LogLevel ToLogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Trace,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        _ => LogLevel.Critical
    };
}
