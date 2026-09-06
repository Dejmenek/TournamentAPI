namespace TournamentAPI.Users;

public static class UserErrorCodes
{
    public const string UserNotFound = "User.NotFound";
    public const string InvalidCredentials = "User.InvalidCredentials";
    public const string RegistrationFailed = "User.RegistrationFailed";
    public const string RefreshTokenExpired = "User.RefreshTokenExpired";
    public const string RefreshTokenInvalid = "User.RefreshTokenInvalid";
    public const string UnableToSetRefreshTokenCookie = "User.UnableToSetRefreshTokenCookie";
    public const string AccountLockedOut = "User.AccountLockedOut";
}
