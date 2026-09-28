using TournamentAPI.Data.Models;

namespace TournamentAPI.Standings;

public class StandingsService
{
    private const int WinPoints = 3;
    private const int DrawPoints = 1;

    public IReadOnlyList<StandingEntry> ComputeStandings(IReadOnlyList<Match> matches)
    {
        var tallies = new Dictionary<int, Tally>();

        foreach (var match in matches)
        {
            var player1 = GetTally(tallies, match.Player1Id);
            var player2 = match.Player2Id is int player2Id ? GetTally(tallies, player2Id) : null;

            if (match.Status == MatchStatus.Played)
            {
                var winnerId = match.WinnerId!.Value;
                var winner = winnerId == match.Player1Id ? player1 : player2!;
                var loser = ReferenceEquals(winner, player1) ? player2 : player1;

                winner.Wins++;

                if (loser is not null)
                    loser.Losses++;
            }
            else if (match.Status == MatchStatus.Drawn)
            {
                player1.Draws++;
                player2!.Draws++;
            }
            else if (match.Status == MatchStatus.Scheduled)
            {
                player1.MatchesRemaining++;

                if (player2 is not null)
                    player2.MatchesRemaining++;
            }
        }

        foreach (var match in matches)
        {
            if (match.Player2Id is not int player2Id)
                continue;

            var player1 = tallies[match.Player1Id];
            var player2 = tallies[player2Id];

            if (player1.Points != player2.Points)
                continue;

            if (match.Status == MatchStatus.Played)
            {
                var winner = match.WinnerId == match.Player1Id ? player1 : player2;
                winner.HeadToHeadScore += WinPoints;
            }
            else if (match.Status == MatchStatus.Drawn)
            {
                player1.HeadToHeadScore += DrawPoints;
                player2.HeadToHeadScore += DrawPoints;
            }
        }

        var ordered = new List<Tally>(tallies.Values);
        ordered.Sort(static (a, b) =>
        {
            var byPoints = b.Points.CompareTo(a.Points);
            if (byPoints != 0)
                return byPoints;

            var byHeadToHead = b.HeadToHeadScore.CompareTo(a.HeadToHeadScore);
            return byHeadToHead != 0 ? byHeadToHead : a.ParticipantId.CompareTo(b.ParticipantId);
        });

        var entries = new List<StandingEntry>(ordered.Count);
        var currentRank = 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            var tally = ordered[i];

            if (i == 0
                || tally.Points != ordered[i - 1].Points
                || tally.HeadToHeadScore != ordered[i - 1].HeadToHeadScore)
            {
                currentRank = i + 1;
            }

            entries.Add(new StandingEntry
            {
                ParticipantId = tally.ParticipantId,
                Wins = tally.Wins,
                Draws = tally.Draws,
                Losses = tally.Losses,
                Points = tally.Points,
                Rank = currentRank,
                MatchesPlayed = tally.Wins + tally.Draws + tally.Losses,
                MatchesRemaining = tally.MatchesRemaining
            });
        }

        return entries;
    }

    private static Tally GetTally(Dictionary<int, Tally> tallies, int participantId)
    {
        if (!tallies.TryGetValue(participantId, out var tally))
        {
            tally = new Tally(participantId);
            tallies.Add(participantId, tally);
        }

        return tally;
    }

    private sealed class Tally(int participantId)
    {
        public int ParticipantId { get; } = participantId;
        public int Wins;
        public int Draws;
        public int Losses;
        public int MatchesRemaining;
        public int HeadToHeadScore;

        public int Points => (Wins * WinPoints) + (Draws * DrawPoints);
    }
}
