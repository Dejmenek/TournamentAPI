using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class MatchMetrics
{
    private readonly Counter<int> _matchesPlayedTotal;
    private readonly Counter<int> _matchesNeedingReplayTotal;
    private readonly Counter<int> _matchResultCorrectionsTotal;

    public MatchMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.MatchMeterName);
        _matchesPlayedTotal = meter.CreateCounter<int>("tournamentapi.matches.matches_played_total", description: "Number of matches played");
        _matchesNeedingReplayTotal = meter.CreateCounter<int>("tournamentapi.matches.matches_needing_replay_total", description: "Number of matches placed into NeedsReplay status by a cascading correction");
        _matchResultCorrectionsTotal = meter.CreateCounter<int>("tournamentapi.matches.match_result_corrections_total", description: "Number of accepted match result corrections");
    }

    public void MatchPlayed() => _matchesPlayedTotal.Add(1);

    public void MatchNeedsReplay() => _matchesNeedingReplayTotal.Add(1);

    public void MatchResultCorrected() => _matchResultCorrectionsTotal.Add(1);
}
