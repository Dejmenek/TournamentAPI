using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.Brackets;

public static class BracketMutationValidations
{
    public static IError? ValidateTournamentExists(Tournament? tournament, int tournamentId)
        => tournament == null ? TournamentErrors.TournamentNotFound(tournamentId) : null;

    public static IError? ValidateBracketExists(Bracket? bracket, int bracketId)
        => bracket == null ? BracketErrors.BracketNotFound(bracketId) : null;

    public static IError? ValidateIsOwner(int ownerId, int userId, int tournamentId)
        => ownerId != userId ? TournamentErrors.TournamentNotOwner(userId, tournamentId) : null;

    public static IError? ValidateTournamentIsClosed(Tournament tournament)
        => tournament.Status != TournamentStatus.Closed ? BracketErrors.BracketGenerationNotAllowed(tournament.Id) : null;

    public static IError? ValidateBracketDoesNotExist(Tournament tournament)
        => tournament.Bracket != null ? BracketErrors.BracketAlreadyExistsForTournament(tournament.Id) : null;

    public static IError? ValidateEnoughParticipants(int participantCount, int tournamentId)
        => participantCount < 2 ? BracketErrors.NotEnoughParticipants(participantCount) : null;

    public static IError? ValidateNextRoundNotGenerated(ICollection<Match> matches, int roundNumber, int bracketId)
        => matches.Any(m => m.Round == roundNumber + 1) ? BracketErrors.NextRoundAlreadyGenerated(bracketId) : null;

    public static IError? ValidateMatchesExistInRound(ICollection<Match> matchesInRound, int roundNumber)
        => matchesInRound.Count == 0 ? BracketErrors.NoMatchesInRound(roundNumber) : null;

    public static IError? ValidateAllMatchesCompleted(ICollection<Match> matchesInRound, int roundNumber)
        => matchesInRound.Any(m => m.WinnerId == null) ? BracketErrors.NotAllMatchesPlayed(roundNumber) : null;

    public static IError? ValidateNotFinalRound(IList<int> winners, int bracketId)
        => winners.Count < 2 ? BracketErrors.BracketAlreadyHasWinner(bracketId) : null;
}
