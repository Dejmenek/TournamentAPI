using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class BracketMetrics
{
    private readonly Counter<int> _bracketGenerationTotal;

    public BracketMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.BracketMeterName);
        _bracketGenerationTotal = meter.CreateCounter<int>("tournamentapi.brackets.bracket_generation_total", description: "Number of bracket generation attempts");
    }

    public void GenerationSucceeded() => _bracketGenerationTotal.Add(1, new KeyValuePair<string, object?>("result", "success"));

    public void GenerationFailed() => _bracketGenerationTotal.Add(1, new KeyValuePair<string, object?>("result", "failure"));
}
