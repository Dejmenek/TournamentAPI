namespace TournamentAPI.Matches;

public static class MatchErrorCodes
{
    public const string MatchNotFound = "Match.NotFound";
    public const string MatchAlreadyPlayed = "Match.AlreadyPlayed";
    public const string InvalidMatchWinner = "Match.InvalidWinner";
    public const string TournamentNotClosed = "Match.TournamentNotClosed";
}
