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

    [Fact]
    public void ValidateTournamentNotFull_WhenAtCapacity_ReturnsError()
    {
        var tournament = new Tournament
        {
            Id = 1,
            MaxParticipants = 2,
            Participants =
            [
                new() { ParticipantId = 1 },
                new() { ParticipantId = 2 }
            ]
        };

        IError? error = TournamentValidations.ValidateTournamentNotFull(tournament);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.TournamentFull, error.Code);
    }

    [Fact]
    public void ValidateTournamentNotFull_WhenBelowCapacity_ReturnsNull()
    {
        var tournament = new Tournament
        {
            Id = 1,
            MaxParticipants = 2,
            Participants =
            [
                new() { ParticipantId = 1 }
            ]
        };

        IError? error = TournamentValidations.ValidateTournamentNotFull(tournament);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ValidateMaxParticipantsAtLeastTwo_WhenLessThanTwo_ReturnsError(int maxParticipants)
    {
        IError? error = TournamentValidations.ValidateMaxParticipantsAtLeastTwo(maxParticipants);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.InvalidMaxParticipants, error.Code);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    public void ValidateMaxParticipantsAtLeastTwo_WhenAtLeastTwo_ReturnsNull(int maxParticipants)
    {
        IError? error = TournamentValidations.ValidateMaxParticipantsAtLeastTwo(maxParticipants);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMaxParticipantsNotBelowParticipantCount_WhenBelowCurrentCount_ReturnsError()
    {
        IError? error = TournamentValidations.ValidateMaxParticipantsNotBelowParticipantCount(tournamentId: 1, currentParticipantCount: 3, maxParticipants: 2);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.MaxParticipantsBelowParticipantCount, error.Code);
    }

    [Fact]
    public void ValidateMaxParticipantsNotBelowParticipantCount_WhenEqualToCurrentCount_ReturnsNull()
    {
        IError? error = TournamentValidations.ValidateMaxParticipantsNotBelowParticipantCount(tournamentId: 1, currentParticipantCount: 3, maxParticipants: 3);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMaxParticipantsNotBelowParticipantCount_WhenAboveCurrentCount_ReturnsNull()
    {
        IError? error = TournamentValidations.ValidateMaxParticipantsNotBelowParticipantCount(tournamentId: 1, currentParticipantCount: 3, maxParticipants: 5);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(-1440)] // a day in the past
    [InlineData(0)]     // exactly now
    [InlineData(29)]    // one minute short of the minimum lead time
    public void ValidateStartDateHasMinimumLeadTime_WhenLeadTimeIsInsufficient_ReturnsError(double minutesFromNow)
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var startDate = now.AddMinutes(minutesFromNow);

        IError? error = TournamentValidations.ValidateStartDateHasMinimumLeadTime(startDate, now);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.StartDateTooSoon, error.Code);
    }

    [Theory]
    [InlineData(30)]    // exactly at the minimum lead time boundary
    [InlineData(10080)] // a week in the future
    public void ValidateStartDateHasMinimumLeadTime_WhenLeadTimeIsSufficient_ReturnsNull(double minutesFromNow)
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var startDate = now.AddMinutes(minutesFromNow);

        IError? error = TournamentValidations.ValidateStartDateHasMinimumLeadTime(startDate, now);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateTournamentCanBeReopened_WhenReopeningWithExistingBracket_ReturnsError()
    {
        IError? error = TournamentValidations.ValidateTournamentCanBeReopened(tournamentId: 1, bracketExists: true, newStatus: TournamentStatus.Open);

        Assert.NotNull(error);
        Assert.Equal(TournamentErrorCodes.CannotReopenTournamentWithBracket, error.Code);
    }

    [Fact]
    public void ValidateTournamentCanBeReopened_WhenReopeningWithoutBracket_ReturnsNull()
    {
        IError? error = TournamentValidations.ValidateTournamentCanBeReopened(tournamentId: 1, bracketExists: false, newStatus: TournamentStatus.Open);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateTournamentCanBeReopened_WhenClosingTournamentWithBracket_ReturnsNull()
    {
        IError? error = TournamentValidations.ValidateTournamentCanBeReopened(tournamentId: 1, bracketExists: true, newStatus: TournamentStatus.Closed);

        Assert.Null(error);
    }
}
