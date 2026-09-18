using TournamentAPI.Data.Models;
using TournamentAPI.Tracing;

namespace TournamentAPI.Brackets;

public class BracketService(ILogger<BracketService> logger)
{
    public Bracket CreateBracket(int tournamentId, IList<int> participantIds)
    {
        using var activity = TournamentActivitySource.Instance.StartActivity("BracketService.CreateBracket");
        activity?.SetTag("tournament.id", tournamentId);
        activity?.SetTag("bracket.participant_count", participantIds.Count);

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

        logger.LogInformation(
            "Bracket generated for tournament {TournamentId}: {ParticipantCount} participants seeded into {MatchCount} first-round matches",
            tournamentId,
            participantIds.Count,
            bracket.Matches.Count);

        return bracket;
    }

    public IList<Match> CreateNextRoundMatches(int bracketId, int roundNumber, IList<int> winners)
    {
        using var activity = TournamentActivitySource.Instance.StartActivity("BracketService.CreateNextRoundMatches");
        activity?.SetTag("bracket.id", bracketId);
        activity?.SetTag("bracket.round", roundNumber + 1);

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

        logger.LogInformation(
            "Round {Round} generated for bracket {BracketId}: {MatchCount} matches",
            roundNumber + 1,
            bracketId,
            matches.Count);

        return matches;
    }
}
