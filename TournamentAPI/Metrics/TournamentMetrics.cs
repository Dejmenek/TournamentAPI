using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class TournamentMetrics
{
    private readonly Counter<int> _tournamentsCreated;
    private readonly UpDownCounter<int> _activeTournaments;

    public TournamentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.TournamentMeterName);
        _tournamentsCreated = meter.CreateCounter<int>("tournamentapi.tournaments.tournaments_created", description: "Number of tournaments created");
        _activeTournaments = meter.CreateUpDownCounter<int>("tournamentapi.tournaments.active_tournaments", description: "Number of currently active tournaments");
    }

    public void IncrementTournamentsCreated() => _tournamentsCreated.Add(1);

    public void TournamentOpened() => _activeTournaments.Add(1);

    public void TournamentClosed() => _activeTournaments.Add(-1);
}
