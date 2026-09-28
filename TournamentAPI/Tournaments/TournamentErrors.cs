namespace TournamentAPI.Tournaments;

public static class TournamentErrors
{
    public static IError TournamentNotFound(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Tournament doesn't exist.")
            .SetCode(TournamentErrorCodes.TournamentNotFound)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError TournamentClosed(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Tournament is closed.")
            .SetCode(TournamentErrorCodes.TournamentClosed)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError UserAlreadyParticipant(int userId, int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("User already participates in the tournament.")
            .SetCode(TournamentErrorCodes.UserAlreadyParticipant)
            .SetExtension("UserId", userId)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError TournamentNameEmpty() =>
        ErrorBuilder.New()
            .SetMessage("Tournament name cannot be empty.")
            .SetCode(TournamentErrorCodes.TournamentNameEmpty)
            .Build();

    public static IError TournamentNotOwner(int userId, int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("User is not the owner of the tournament.")
            .SetCode(TournamentErrorCodes.TournamentNotOwner)
            .SetExtension("UserId", userId)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError TournamentFull(int tournamentId, int maxParticipants) =>
        ErrorBuilder.New()
            .SetMessage("Tournament has reached its maximum number of participants.")
            .SetCode(TournamentErrorCodes.TournamentFull)
            .SetExtension("TournamentId", tournamentId)
            .SetExtension("MaxParticipants", maxParticipants)
            .Build();

    public static IError InvalidMaxParticipants(int maxParticipants) =>
        ErrorBuilder.New()
            .SetMessage("MaxParticipants must be at least 2.")
            .SetCode(TournamentErrorCodes.InvalidMaxParticipants)
            .SetExtension("MaxParticipants", maxParticipants)
            .Build();

    public static IError MaxParticipantsBelowParticipantCount(int tournamentId, int maxParticipants, int currentParticipantCount) =>
        ErrorBuilder.New()
            .SetMessage("MaxParticipants cannot be lower than the current number of participants.")
            .SetCode(TournamentErrorCodes.MaxParticipantsBelowParticipantCount)
            .SetExtension("TournamentId", tournamentId)
            .SetExtension("MaxParticipants", maxParticipants)
            .SetExtension("CurrentParticipantCount", currentParticipantCount)
            .Build();

    public static IError StartDateTooSoon(DateTime startDate) =>
        ErrorBuilder.New()
            .SetMessage($"StartDate must be at least {TournamentValidations.MinimumStartDateLeadTime.TotalMinutes} minutes from now.")
            .SetCode(TournamentErrorCodes.StartDateTooSoon)
            .SetExtension("StartDate", startDate)
            .Build();

    public static IError CannotReopenTournamentWithBracket(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Tournament cannot be reopened because a bracket already exists.")
            .SetCode(TournamentErrorCodes.CannotReopenTournamentWithBracket)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError CannotReopenTournamentAfterStartDate(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Tournament cannot be reopened because its StartDate has already passed.")
            .SetCode(TournamentErrorCodes.CannotReopenTournamentAfterStartDate)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError CannotDeleteTournamentWithBracket(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Tournament cannot be deleted because it is closed and already has a bracket in play.")
            .SetCode(TournamentErrorCodes.CannotDeleteTournamentWithBracket)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError TournamentStatusCannotBeSetManually() =>
        ErrorBuilder.New()
            .SetMessage("Tournament status cannot be manually set to Completed; it is set automatically once the final match is decided.")
            .SetCode(TournamentErrorCodes.StatusCannotBeSetManually)
            .Build();

    public static IError CannotChangeCompletedTournamentStatus(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Tournament status cannot be changed once the tournament is Completed.")
            .SetCode(TournamentErrorCodes.CannotChangeCompletedStatus)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError NotAuthorizedForWithdrawal(int userId, int tournamentId, int participantId) =>
        ErrorBuilder.New()
            .SetMessage("Only the tournament owner or the participant being withdrawn may perform this action.")
            .SetCode(TournamentErrorCodes.NotAuthorizedForWithdrawal)
            .SetExtension("UserId", userId)
            .SetExtension("TournamentId", tournamentId)
            .SetExtension("ParticipantId", participantId)
            .Build();

    public static IError WithdrawalNotSupportedForFormat(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Participant withdrawal is only supported for Round Robin tournaments.")
            .SetCode(TournamentErrorCodes.WithdrawalNotSupportedForFormat)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError WithdrawalRequiresBracket(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Participant withdrawal requires the tournament's bracket to already be generated.")
            .SetCode(TournamentErrorCodes.WithdrawalRequiresBracket)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError ParticipantNotFound(int tournamentId, int participantId) =>
        ErrorBuilder.New()
            .SetMessage("Participant was not found in this tournament.")
            .SetCode(TournamentErrorCodes.ParticipantNotFound)
            .SetExtension("TournamentId", tournamentId)
            .SetExtension("ParticipantId", participantId)
            .Build();
}
