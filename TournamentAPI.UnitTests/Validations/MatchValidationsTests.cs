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
    public void ValidateMatchNotPlayed_WhenMatchIsPlayed_ReturnsError()
    {
        var match = new Match { Id = 1, WinnerId = 5, Status = MatchStatus.Played };

        IError? error = MatchValidations.ValidateMatchNotPlayed(match);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.MatchAlreadyPlayed, error.Code);
    }

    [Fact]
    public void ValidateMatchNotPlayed_WhenMatchIsScheduled_ReturnsNull()
    {
        var match = new Match { Id = 1, WinnerId = null, Status = MatchStatus.Scheduled };

        IError? error = MatchValidations.ValidateMatchNotPlayed(match);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMatchNotPlayed_WhenMatchNeedsReplay_ReturnsNull()
    {
        var match = new Match { Id = 1, WinnerId = 5, Status = MatchStatus.NeedsReplay };

        IError? error = MatchValidations.ValidateMatchNotPlayed(match);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMatchNotScheduled_WhenMatchIsScheduled_ReturnsError()
    {
        var match = new Match { Id = 1, Status = MatchStatus.Scheduled };

        IError? error = MatchValidations.ValidateMatchNotScheduled(match);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.MatchNotYetPlayed, error.Code);
    }

    [Fact]
    public void ValidateMatchNotScheduled_WhenMatchIsPlayed_ReturnsNull()
    {
        var match = new Match { Id = 1, Status = MatchStatus.Played };

        IError? error = MatchValidations.ValidateMatchNotScheduled(match);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMatchNotScheduled_WhenMatchNeedsReplay_ReturnsNull()
    {
        var match = new Match { Id = 1, Status = MatchStatus.NeedsReplay };

        IError? error = MatchValidations.ValidateMatchNotScheduled(match);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMatchNotNeedsReplay_WhenMatchNeedsReplay_ReturnsError()
    {
        var match = new Match { Id = 1, Status = MatchStatus.NeedsReplay };

        IError? error = MatchValidations.ValidateMatchNotNeedsReplay(match);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.MatchNeedsReplay, error.Code);
    }

    [Fact]
    public void ValidateMatchNotNeedsReplay_WhenMatchIsScheduled_ReturnsNull()
    {
        var match = new Match { Id = 1, Status = MatchStatus.Scheduled };

        IError? error = MatchValidations.ValidateMatchNotNeedsReplay(match);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMatchNotNeedsReplay_WhenMatchIsPlayed_ReturnsNull()
    {
        var match = new Match { Id = 1, Status = MatchStatus.Played };

        IError? error = MatchValidations.ValidateMatchNotNeedsReplay(match);

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

    [Fact]
    public void ValidateScoresAreNonNegative_WhenPlayer1ScoreIsNegative_ReturnsError()
    {
        IError? error = MatchValidations.ValidateScoresAreNonNegative(1, -1, 2);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.NegativeScore, error.Code);
    }

    [Fact]
    public void ValidateScoresAreNonNegative_WhenPlayer2ScoreIsNegative_ReturnsError()
    {
        IError? error = MatchValidations.ValidateScoresAreNonNegative(1, 2, -1);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.NegativeScore, error.Code);
    }

    [Fact]
    public void ValidateScoresAreNonNegative_WhenBothScoresAreNonNegative_ReturnsNull()
    {
        IError? error = MatchValidations.ValidateScoresAreNonNegative(1, 3, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWinnerHasHigherScore_WhenScoresAreTied_ReturnsError()
    {
        var match = new Match { Id = 1, Player1Id = 2, Player2Id = 3 };

        IError? error = MatchValidations.ValidateWinnerHasHigherScore(match, 2, 1, 1);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.WinnerScoreMismatch, error.Code);
    }

    [Fact]
    public void ValidateWinnerHasHigherScore_WhenWinnerScoreIsLower_ReturnsError()
    {
        var match = new Match { Id = 1, Player1Id = 2, Player2Id = 3 };

        IError? error = MatchValidations.ValidateWinnerHasHigherScore(match, 2, 1, 3);

        Assert.NotNull(error);
        Assert.Equal(MatchErrorCodes.WinnerScoreMismatch, error.Code);
    }

    [Fact]
    public void ValidateWinnerHasHigherScore_WhenPlayer1IsWinnerWithHigherScore_ReturnsNull()
    {
        var match = new Match { Id = 1, Player1Id = 2, Player2Id = 3 };

        IError? error = MatchValidations.ValidateWinnerHasHigherScore(match, 2, 3, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWinnerHasHigherScore_WhenPlayer2IsWinnerWithHigherScore_ReturnsNull()
    {
        var match = new Match { Id = 1, Player1Id = 2, Player2Id = 3 };

        IError? error = MatchValidations.ValidateWinnerHasHigherScore(match, 3, 1, 3);

        Assert.Null(error);
    }
}
