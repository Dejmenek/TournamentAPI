using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

public static class UserValidations
{
    public static IError? ValidateUserExists(ApplicationUser? user, int userId)
       => user == null ? UserErrors.UserNotFound(userId) : null;

    public static IError? ValidateCredentials(ApplicationUser? user)
        => user == null ? UserErrors.InvalidCredentials() : null;
}
