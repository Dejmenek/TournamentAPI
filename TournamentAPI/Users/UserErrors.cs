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

    public static IError RefreshTokenExpired() =>
        ErrorBuilder.New()
            .SetMessage("The refresh token has expired.")
            .SetCode(UserErrorCodes.RefreshTokenExpired)
            .Build();

    public static IError RefreshTokenInvalid() =>
        ErrorBuilder.New()
            .SetMessage("The refresh token is invalid.")
            .SetCode(UserErrorCodes.RefreshTokenInvalid)
            .Build();

    public static IError UnableToSetRefreshTokenCookie() =>
        ErrorBuilder.New()
            .SetMessage("Unable to set refresh token cookie.")
            .SetCode(UserErrorCodes.UnableToSetRefreshTokenCookie)
            .Build();
}