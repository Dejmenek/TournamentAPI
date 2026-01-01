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
}
