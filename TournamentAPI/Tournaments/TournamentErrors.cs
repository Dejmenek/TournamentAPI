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
}
