using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class ParticipantMetrics
{
    private readonly Counter<int> _tournamentJoinAttemptsTotal;
    private readonly Counter<int> _slotContentionTotal;

    public ParticipantMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.ParticipantMeterName);
        _tournamentJoinAttemptsTotal = meter.CreateCounter<int>("tournamentapi.participants.tournament_join_attempts_total", description: "Number of self-service tournament join attempts");
        _slotContentionTotal = meter.CreateCounter<int>("tournamentapi.participants.slot_contention_total", description: "Number of participant-slot unique-constraint races");
    }

    public void JoinAttempt(string result) => _tournamentJoinAttemptsTotal.Add(1, new KeyValuePair<string, object?>("result", result));

    public void SlotContention() => _slotContentionTotal.Add(1);
}
