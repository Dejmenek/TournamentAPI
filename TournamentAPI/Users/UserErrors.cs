namespace TournamentAPI.Users;

public static class UserErrors
{
    public static IError UserNotFound(int userId) =>
        ErrorBuilder.New()
            .SetMessage("The specified user was not found.")
            .SetCode(UserErrorCodes.UserNotFound)
            .SetExtension("UserId", userId)
            .Build();

    public static IError InvalidCredentials() =>
        ErrorBuilder.New()
            .SetMessage("The provided credentials are invalid.")
            .SetCode(UserErrorCodes.InvalidCredentials)
            .Build();

    public static IError RegistrationFailed(string[] errors) =>
        ErrorBuilder.New()
            .SetMessage("User registration failed.")
            .SetCode(UserErrorCodes.RegistrationFailed)
            .SetExtension("Errors", errors)
            .Build();
}