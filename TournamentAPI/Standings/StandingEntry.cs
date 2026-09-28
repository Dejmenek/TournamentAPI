namespace TournamentAPI.Standings;

public class StandingEntry
{
    public int ParticipantId { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int Points { get; set; }
    public int Rank { get; set; }
    public int MatchesPlayed { get; set; }
    public int MatchesRemaining { get; set; }
}
