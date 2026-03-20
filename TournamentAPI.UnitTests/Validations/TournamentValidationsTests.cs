using HotChocolate;
using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.UnitTests.Validations;

public class TournamentValidationsTests
{
    [Fact]
    public void ValidateTournamentExists_WhenTournamentIsNull_ReturnsError()
    {
        IError? error = TournamentValidations.ValidateTournamentExists(null, 1);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.TournamentNotFound, error.Code);
    }

    [Fact]
    public void ValidateTournamentExists_WhenTournamentExists_ReturnsNull()
    {
        var tournament = new Tournament { Id = 1 };

        IError? error = TournamentValidations.ValidateTournamentExists(tournament, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateIsOwner_WhenUserIsNotOwner_ReturnsError()
    {
        IError? error = TournamentValidations.ValidateIsOwner(ownerId: 1, userId: 2, tournamentId: 10);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.TournamentNotOwner, error.Code);
    }

    [Fact]
    public void ValidateIsOwner_WhenUserIsOwner_ReturnsNull()
    {
        IError? error = TournamentValidations.ValidateIsOwner(ownerId: 1, userId: 1, tournamentId: 10);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateTournamentIsNotClosed_WhenTournamentIsClosed_ReturnsError()
    {
        var tournament = new Tournament { Id = 1, Status = TournamentStatus.Closed };

        IError? error = TournamentValidations.ValidateTournamentIsNotClosed(tournament);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.TournamentClosed, error.Code);
    }

    [Fact]
    public void ValidateTournamentIsNotClosed_WhenTournamentIsOpen_ReturnsNull()
    {
        var tournament = new Tournament { Id = 1, Status = TournamentStatus.Open };

        IError? error = TournamentValidations.ValidateTournamentIsNotClosed(tournament);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateUserNotAlreadyParticipant_WhenUserIsAlreadyParticipant_ReturnsError()
    {
        var tournament = new Tournament
        {
            Id = 1,
            Participants =
            [
                new() { ParticipantId = 5 }
            ]
        };

        IError? error = TournamentValidations.ValidateUserNotAlreadyParticipant(tournament, 5);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.UserAlreadyParticipant, error.Code);
    }

    [Fact]
    public void ValidateUserNotAlreadyParticipant_WhenUserIsNotParticipant_ReturnsNull()
    {
        var tournament = new Tournament
        {
            Id = 1,
            Participants =
            [
                new() { ParticipantId = 5 }
            ]
        };

        IError? error = TournamentValidations.ValidateUserNotAlreadyParticipant(tournament, 99);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTournamentNameNotEmpty_WhenNameIsNullOrWhitespace_ReturnsError(string? name)
    {
        IError? error = TournamentValidations.ValidateTournamentNameNotEmpty(name);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.TournamentNameEmpty, error.Code);
    }

    [Fact]
    public void ValidateTournamentNameNotEmpty_WhenNameIsValid_ReturnsNull()
    {
        IError? error = TournamentValidations.ValidateTournamentNameNotEmpty("Spring Open");

        Assert.Null(error);
    }
}
