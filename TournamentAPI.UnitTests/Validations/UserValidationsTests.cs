using HotChocolate;
using TournamentAPI.Data.Models;
using TournamentAPI.Users;

namespace TournamentAPI.UnitTests.Validations;

public class UserValidationsTests
{
    [Fact]
    public void ValidateUserExists_WhenUserIsNull_ReturnsError()
    {
        IError? error = UserValidations.ValidateUserExists(null, 1);

        Assert.NotNull(error);
        Assert.Equal(UserErrorCodes.UserNotFound, error.Code);
    }

    [Fact]
    public void ValidateUserExists_WhenUserExists_ReturnsNull()
    {
        var user = new ApplicationUser { Id = 1 };

        IError? error = UserValidations.ValidateUserExists(user, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateCredentials_WhenUserIsNull_ReturnsError()
    {
        IError? error = UserValidations.ValidateCredentials(null);

        Assert.NotNull(error);
        Assert.Equal(UserErrorCodes.InvalidCredentials, error.Code);
    }

    [Fact]
    public void ValidateCredentials_WhenUserExists_ReturnsNull()
    {
        var user = new ApplicationUser { Id = 1 };

        IError? error = UserValidations.ValidateCredentials(user);

        Assert.Null(error);
    }
}
