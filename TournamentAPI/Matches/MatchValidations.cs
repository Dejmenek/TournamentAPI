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
}
