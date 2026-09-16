using TournamentAPI.Data.Models;

namespace TournamentAPI.Brackets;

public static class BracketService
{
    public static Bracket CreateBracket(int tournamentId, IList<int> participantIds)
    {
        var bracket = new Bracket
        {
            TournamentId = tournamentId,
            Matches = new List<Match>()
        };

        var shuffled = participantIds.OrderBy(_ => Random.Shared.Next()).ToList();

        for (int i = 0; i < shuffled.Count; i += 2)
        {
            var isBye = i + 1 >= shuffled.Count;

            bracket.Matches.Add(new Match
            {
                Round = 1,
                Player1Id = shuffled[i],
                Player2Id = isBye ? null : shuffled[i + 1],
                Bracket = bracket,
                WinnerId = isBye ? shuffled[i] : null,
                Status = isBye ? MatchStatus.Played : MatchStatus.Scheduled
            });
        }

        return bracket;
    }

    public static IList<Match> CreateNextRoundMatches(int bracketId, int roundNumber, IList<int> winners)
    {
        var matches = new List<Match>();

        for (int i = 0; i < winners.Count; i += 2)
        {
            int p1 = winners[i];
            int? p2 = i + 1 < winners.Count ? winners[i + 1] : null;

            if (p2 != null && p2 < p1)
                (p1, p2) = (p2.Value, p1);

            var isBye = p2 == null;

            matches.Add(new Match
            {
                BracketId = bracketId,
                Round = roundNumber + 1,
                Player1Id = p1,
                Player2Id = p2,
                WinnerId = isBye ? p1 : null,
                Status = isBye ? MatchStatus.Played : MatchStatus.Scheduled
            });
        }

        return matches;
    }
}
