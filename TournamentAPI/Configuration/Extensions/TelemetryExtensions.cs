using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TournamentAPI.Metrics;

namespace TournamentAPI.Configuration.Extensions;

internal static class TelemetryExtensions
{
    internal static IServiceCollection AddApplicationTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("TournamentAPI"))
            .WithTracing(tracing =>
            {
                tracing.AddHttpClientInstrumentation();
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHotChocolateInstrumentation();
                tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(MetricConstants.TournamentMeterName);
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri("http://localhost:5431/api/v1/otlp/v1/metrics");
                    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                });
            });

        return services;
    }
}
