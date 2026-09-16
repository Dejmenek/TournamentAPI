namespace TournamentAPI.Matches;

public static class MatchCascadePositionCalculator
{
    public static int? GetDownstreamMatchId(
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

        return nextRoundMatchIdsAscending[downstreamPosition];
    }
}
