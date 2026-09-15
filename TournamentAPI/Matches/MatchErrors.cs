namespace TournamentAPI.Matches;

public static class MatchErrors
{
    public static IError MatchNotFound(int matchId) =>
        ErrorBuilder.New()
            .SetMessage("Match was not found.")
            .SetCode(MatchErrorCodes.MatchNotFound)
            .SetExtension("MatchId", matchId)
            .Build();

    public static IError MatchAlreadyPlayed(int matchId) =>
        ErrorBuilder.New()
            .SetMessage("Match has already been played.")
            .SetCode(MatchErrorCodes.MatchAlreadyPlayed)
            .SetExtension("MatchId", matchId)
            .Build();

    public static IError InvalidMatchWinner(int matchId, int winnerId) =>
        ErrorBuilder.New()
            .SetMessage("Winner must be one of the match participants.")
            .SetCode(MatchErrorCodes.InvalidMatchWinner)
            .SetExtension("MatchId", matchId)
            .SetExtension("WinnerId", winnerId)
            .Build();

    public static IError TournamentNotClosed(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Matches can only be played when the tournament is closed.")
            .SetCode(MatchErrorCodes.TournamentNotClosed)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError NegativeScore(int matchId, int player1Score, int player2Score) =>
        ErrorBuilder.New()
            .SetMessage("Match scores cannot be negative.")
            .SetCode(MatchErrorCodes.NegativeScore)
            .SetExtension("MatchId", matchId)
            .SetExtension("Player1Score", player1Score)
            .SetExtension("Player2Score", player2Score)
            .Build();

    public static IError WinnerScoreMismatch(int matchId, int winnerId) =>
        ErrorBuilder.New()
            .SetMessage("The declared winner's score must be strictly greater than the opponent's score.")
            .SetCode(MatchErrorCodes.WinnerScoreMismatch)
            .SetExtension("MatchId", matchId)
            .SetExtension("WinnerId", winnerId)
            .Build();
}
