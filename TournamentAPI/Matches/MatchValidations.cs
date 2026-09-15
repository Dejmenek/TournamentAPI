using TournamentAPI.Data.Models;

namespace TournamentAPI.Matches;

public static class MatchValidations
{
    public static IError? ValidateMatchExists(Match? match, int matchId)
        => match == null ? MatchErrors.MatchNotFound(matchId) : null;

    public static IError? ValidateTournamentIsClosed(Tournament tournament)
        => tournament.Status != TournamentStatus.Closed ? MatchErrors.TournamentNotClosed(tournament.Id) : null;

    public static IError? ValidateMatchNotPlayed(Match match)
        => match.WinnerId != null ? MatchErrors.MatchAlreadyPlayed(match.Id) : null;

    public static IError? ValidateWinnerIsParticipant(Match match, int winnerId)
        => winnerId != match.Player1Id && winnerId != match.Player2Id ? MatchErrors.InvalidMatchWinner(match.Id, winnerId) : null;

    public static IError? ValidateScoresAreNonNegative(int matchId, int player1Score, int player2Score)
        => player1Score < 0 || player2Score < 0 ? MatchErrors.NegativeScore(matchId, player1Score, player2Score) : null;

    public static IError? ValidateWinnerHasHigherScore(Match match, int winnerId, int player1Score, int player2Score)
    {
        var winnerScore = winnerId == match.Player1Id ? player1Score : player2Score;
        var opponentScore = winnerId == match.Player1Id ? player2Score : player1Score;
        return winnerScore <= opponentScore ? MatchErrors.WinnerScoreMismatch(match.Id, winnerId) : null;
    }
}
