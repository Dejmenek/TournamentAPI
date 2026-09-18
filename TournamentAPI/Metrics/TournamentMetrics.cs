using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class TournamentMetrics
{
    private readonly Counter<int> _tournamentsCreated;
    private readonly UpDownCounter<int> _activeTournaments;
    private readonly Counter<int> _tournamentsClosedTotal;
    private readonly Counter<int> _tournamentsDeletedTotal;

    public TournamentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.TournamentMeterName);
        _tournamentsCreated = meter.CreateCounter<int>("tournamentapi.tournaments.tournaments_created", description: "Number of tournaments created");
        _activeTournaments = meter.CreateUpDownCounter<int>("tournamentapi.tournaments.active_tournaments", description: "Number of currently active tournaments");
        _tournamentsClosedTotal = meter.CreateCounter<int>("tournamentapi.tournaments.tournaments_closed_total", description: "Number of tournaments closed, tagged by reason");
        _tournamentsDeletedTotal = meter.CreateCounter<int>("tournamentapi.tournaments.tournaments_deleted_total", description: "Number of tournaments deleted");
    }

    public void IncrementTournamentsCreated() => _tournamentsCreated.Add(1);

    public void TournamentOpened() => _activeTournaments.Add(1);

    public void TournamentClosed() => _activeTournaments.Add(-1);

    public void IncrementTournamentsClosed(string reason, int count = 1) =>
        _tournamentsClosedTotal.Add(count, new KeyValuePair<string, object?>("reason", reason));

    public void IncrementTournamentsDeleted() => _tournamentsDeletedTotal.Add(1);
}
