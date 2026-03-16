using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public static class TournamentValidations
{
    public static IError? ValidateTournamentExists(Tournament? tournament, int tournamentId)
        => tournament == null ? TournamentErrors.TournamentNotFound(tournamentId) : null;

    public static IError? ValidateIsOwner(int ownerId, int userId, int tournamentId)
        => ownerId != userId ? TournamentErrors.TournamentNotOwner(userId, tournamentId) : null;

    public static IError? ValidateTournamentIsNotClosed(Tournament tournament)
        => tournament.Status == TournamentStatus.Closed ? TournamentErrors.TournamentClosed(tournament.Id) : null;

    public static IError? ValidateUserNotAlreadyParticipant(Tournament tournament, int userId)
        => tournament.Participants.Any(tp => tp.ParticipantId == userId) ? TournamentErrors.UserAlreadyParticipant(userId, tournament.Id) : null;

    public static IError? ValidateTournamentNameNotEmpty(string? name)
        => string.IsNullOrWhiteSpace(name) ? TournamentErrors.TournamentNameEmpty() : null;
}
