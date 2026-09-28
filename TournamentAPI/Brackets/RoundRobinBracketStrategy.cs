using TournamentAPI.Data.Models;

namespace TournamentAPI.Brackets;

public class RoundRobinBracketStrategy : IBracketGenerationStrategy
{
    private const int ByeSeat = -1;

    public TournamentFormat Format => TournamentFormat.RoundRobin;

    public Bracket CreateBracket(int tournamentId, IList<int> participantIds)
    {
        var bracket = new Bracket
        {
            TournamentId = tournamentId,
            Matches = new List<Match>()
        };

        var rotation = new List<int>(participantIds);
        if (rotation.Count % 2 != 0)
            rotation.Add(ByeSeat);

        var seatCount = rotation.Count;
        var roundCount = seatCount - 1;

        for (var round = 1; round <= roundCount; round++)
        {
            for (var i = 0; i < seatCount / 2; i++)
            {
                var seatA = rotation[i];
                var seatB = rotation[seatCount - 1 - i];

                if (seatA == ByeSeat || seatB == ByeSeat)
                    continue;

                bracket.Matches.Add(new Match
                {
                    Round = round,
                    Player1Id = seatA,
                    Player2Id = seatB,
                    Bracket = bracket,
                    WinnerId = null,
                    Status = MatchStatus.Scheduled
                });
            }

            var lastSeat = rotation[seatCount - 1];
            for (var i = seatCount - 1; i > 1; i--)
                rotation[i] = rotation[i - 1];
            rotation[1] = lastSeat;
        }

        return bracket;
    }
}
