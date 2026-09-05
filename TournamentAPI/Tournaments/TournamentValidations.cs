using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public static class TournamentValidations
{
    public static readonly TimeSpan MinimumStartDateLeadTime = TimeSpan.FromMinutes(30);

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

    public static IError? ValidateTournamentNotFull(Tournament tournament)
        => tournament.Participants.Count >= tournament.MaxParticipants ? TournamentErrors.TournamentFull(tournament.Id, tournament.MaxParticipants) : null;

    public static IError? ValidateMaxParticipantsAtLeastTwo(int maxParticipants)
        => maxParticipants < 2 ? TournamentErrors.InvalidMaxParticipants(maxParticipants) : null;

    public static IError? ValidateMaxParticipantsNotBelowParticipantCount(int tournamentId, int currentParticipantCount, int maxParticipants)
        => maxParticipants < currentParticipantCount ? TournamentErrors.MaxParticipantsBelowParticipantCount(tournamentId, maxParticipants, currentParticipantCount) : null;

    public static IError? ValidateStartDateHasMinimumLeadTime(DateTime startDate, DateTime now)
        => startDate < now.Add(MinimumStartDateLeadTime) ? TournamentErrors.StartDateTooSoon(startDate) : null;

    public static IError? ValidateTournamentCanBeReopened(int tournamentId, bool bracketExists, TournamentStatus newStatus)
        => newStatus == TournamentStatus.Open && bracketExists ? TournamentErrors.CannotReopenTournamentWithBracket(tournamentId) : null;
}
