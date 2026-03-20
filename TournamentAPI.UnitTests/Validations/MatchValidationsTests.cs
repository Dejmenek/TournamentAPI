using HotChocolate;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;

namespace TournamentAPI.UnitTests.Validations;

public class MatchValidationsTests
{
    [Fact]
    public void ValidateMatchExists_WhenMatchIsNull_ReturnsError()
    {
        IError? error = MatchValidations.ValidateMatchExists(null, 1);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.MatchNotFound, error.Code);
    }

    [Fact]
    public void ValidateMatchExists_WhenMatchExists_ReturnsNull()
    {
        var match = new Match { Id = 1 };

        IError? error = MatchValidations.ValidateMatchExists(match, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateTournamentIsClosed_WhenTournamentIsOpen_ReturnsError()
    {
        var tournament = new Tournament { Id = 1, Status = TournamentStatus.Open };

        IError? error = MatchValidations.ValidateTournamentIsClosed(tournament);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.TournamentNotClosed, error.Code);
    }

    [Fact]
    public void ValidateTournamentIsClosed_WhenTournamentIsClosed_ReturnsNull()
    {
        var tournament = new Tournament { Id = 1, Status = TournamentStatus.Closed };

        IError? error = MatchValidations.ValidateTournamentIsClosed(tournament);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMatchNotPlayed_WhenMatchAlreadyHasWinner_ReturnsError()
    {
        var match = new Match { Id = 1, WinnerId = 5 };

        IError? error = MatchValidations.ValidateMatchNotPlayed(match);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.MatchAlreadyPlayed, error.Code);
    }

    [Fact]
    public void ValidateMatchNotPlayed_WhenMatchHasNoWinner_ReturnsNull()
    {
        var match = new Match { Id = 1, WinnerId = null };

        IError? error = MatchValidations.ValidateMatchNotPlayed(match);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWinnerIsParticipant_WhenWinnerIsNotAParticipant_ReturnsError()
    {
        var match = new Match { Id = 1, Player1Id = 2, Player2Id = 3 };

        IError? error = MatchValidations.ValidateWinnerIsParticipant(match, 99);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.InvalidMatchWinner, error.Code);
    }

    [Fact]
    public void ValidateWinnerIsParticipant_WhenWinnerIsPlayer1_ReturnsNull()
    {
        var match = new Match { Id = 1, Player1Id = 2, Player2Id = 3 };

        IError? error = MatchValidations.ValidateWinnerIsParticipant(match, 2);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWinnerIsParticipant_WhenWinnerIsPlayer2_ReturnsNull()
    {
        var match = new Match { Id = 1, Player1Id = 2, Player2Id = 3 };

        IError? error = MatchValidations.ValidateWinnerIsParticipant(match, 3);

        Assert.Null(error);
    }
}
