namespace TournamentAPI.Matches;

public class MatchCascadePositionCalculator(ILogger<MatchCascadePositionCalculator> logger)
{
    public int? GetDownstreamMatchId(
        IReadOnlyList<int> currentRoundMatchIdsAscending,
        IReadOnlyList<int> nextRoundMatchIdsAscending,
        int matchId)
    {
        var position = -1;
        for (var i = 0; i < currentRoundMatchIdsAscending.Count; i++)
        {
            if (currentRoundMatchIdsAscending[i] == matchId)
            {
                position = i;
                break;
            }
        }

        if (position < 0)
            return null;

        var downstreamPosition = position / 2;
        if (downstreamPosition >= nextRoundMatchIdsAscending.Count)
            return null;

        var downstreamMatchId = nextRoundMatchIdsAscending[downstreamPosition];

        logger.LogInformation(
            "Cascade applied: match {MatchId} maps to downstream match {DownstreamMatchId}",
            matchId,
            downstreamMatchId);

        return downstreamMatchId;
    }
}
