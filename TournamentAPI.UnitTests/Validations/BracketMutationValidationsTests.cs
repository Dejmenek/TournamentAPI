using HotChocolate;
using TournamentAPI.Brackets;
using TournamentAPI.Data.Models;

namespace TournamentAPI.UnitTests.Validations;

public class BracketMutationValidationsTests
{
    [Fact]
    public void ValidateBracketExists_WhenBracketIsNull_ReturnsError()
    {
        IError? error = BracketMutationValidations.ValidateBracketExists(null, 1);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.BracketNotFound, error.Code);
    }

    [Fact]
    public void ValidateBracketExists_WhenBracketExists_ReturnsNull()
    {
        var bracket = new Bracket { Id = 1 };

        IError? error = BracketMutationValidations.ValidateBracketExists(bracket, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateTournamentIsClosed_WhenTournamentIsOpen_ReturnsError()
    {
        var tournament = new Tournament { Id = 1, Status = TournamentStatus.Open };

        IError? error = BracketMutationValidations.ValidateTournamentIsClosed(tournament);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.BracketGenerationNotAllowed, error.Code);
    }

    [Fact]
    public void ValidateTournamentIsClosed_WhenTournamentIsClosed_ReturnsNull()
    {
        var tournament = new Tournament { Id = 1, Status = TournamentStatus.Closed };

        IError? error = BracketMutationValidations.ValidateTournamentIsClosed(tournament);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateBracketDoesNotExist_WhenBracketExists_ReturnsError()
    {
        var tournament = new Tournament { Id = 1, Bracket = new Bracket() };

        IError? error = BracketMutationValidations.ValidateBracketDoesNotExist(tournament);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.BracketAlreadyExists, error.Code);
    }

    [Fact]
    public void ValidateBracketDoesNotExist_WhenBracketDoesNotExist_ReturnsNull()
    {
        var tournament = new Tournament { Id = 1, Bracket = null };

        IError? error = BracketMutationValidations.ValidateBracketDoesNotExist(tournament);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ValidateEnoughParticipants_WhenCountIsLessThanTwo_ReturnsError(int count)
    {
        IError? error = BracketMutationValidations.ValidateEnoughParticipants(count, 1);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.NotEnoughParticipants, error.Code);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    public void ValidateEnoughParticipants_WhenCountIsAtLeastTwo_ReturnsNull(int count)
    {
        IError? error = BracketMutationValidations.ValidateEnoughParticipants(count, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateNextRoundNotGenerated_WhenNextRoundExists_ReturnsError()
    {
        var matches = new List<Match>
        {
            new() { Round = 1 },
            new() { Round = 2 }
        };

        IError? error = BracketMutationValidations.ValidateNextRoundNotGenerated(matches, 1, 1);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.NextRoundAlreadyGenerated, error.Code);
    }

    [Fact]
    public void ValidateNextRoundNotGenerated_WhenNextRoundDoesNotExist_ReturnsNull()
    {
        var matches = new List<Match>
        {
            new() { Round = 1 }
        };

        IError? error = BracketMutationValidations.ValidateNextRoundNotGenerated(matches, 1, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateMatchesExistInRound_WhenNoMatchesInRound_ReturnsError()
    {
        IError? error = BracketMutationValidations.ValidateMatchesExistInRound([], 1);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.NoMatchesInRound, error.Code);
    }

    [Fact]
    public void ValidateMatchesExistInRound_WhenMatchesExist_ReturnsNull()
    {
        var matches = new List<Match> { new() { Round = 1 } };

        IError? error = BracketMutationValidations.ValidateMatchesExistInRound(matches, 1);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateAllMatchesCompleted_WhenSomeMatchesHaveNoWinner_ReturnsError()
    {
        var matches = new List<Match>
        {
            new() { WinnerId = 1 },
            new() { WinnerId = null }
        };

        IError? error = BracketMutationValidations.ValidateAllMatchesCompleted(matches, 1);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.NotAllMatchesPlayed, error.Code);
    }

    [Fact]
    public void ValidateAllMatchesCompleted_WhenAllMatchesHaveWinner_ReturnsNull()
    {
        var matches = new List<Match>
        {
            new() { WinnerId = 1 },
            new() { WinnerId = 2 }
        };

        IError? error = BracketMutationValidations.ValidateAllMatchesCompleted(matches, 1);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ValidateNotFinalRound_WhenFewerThanTwoWinners_ReturnsError(int winnerCount)
    {
        var winners = Enumerable.Range(1, winnerCount).ToList();

        IError? error = BracketMutationValidations.ValidateNotFinalRound(winners, 1);

        Assert.NotNull(error);
        Assert.Equal(BracketErrorCodes.BracketAlreadyHasWinner, error.Code);
    }

    [Fact]
    public void ValidateNotFinalRound_WhenMultipleWinners_ReturnsNull()
    {
        var winners = new List<int> { 1, 2 };

        IError? error = BracketMutationValidations.ValidateNotFinalRound(winners, 1);

        Assert.Null(error);
    }
}
