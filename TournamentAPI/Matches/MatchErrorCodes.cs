namespace TournamentAPI.Matches;

public static class MatchErrorCodes
{
    public const string MatchNotFound = "Match.NotFound";
    public const string MatchAlreadyPlayed = "Match.AlreadyPlayed";
    public const string InvalidMatchWinner = "Match.InvalidWinner";
    public const string TournamentNotClosed = "Match.TournamentNotClosed";
    public const string NegativeScore = "Match.NegativeScore";
    public const string WinnerScoreMismatch = "Match.WinnerScoreMismatch";
    public const string MatchNotYetPlayed = "Match.NotYetPlayed";
    public const string MatchNeedsReplay = "Match.NeedsReplay";
    public const string InvalidVersionToken = "Match.InvalidVersionToken";
    public const string MatchVersionConflict = "Match.VersionConflict";
    public const string MatchCorrectionFailed = "Match.CorrectionFailed";
}
