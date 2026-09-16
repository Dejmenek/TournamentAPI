using TournamentAPI.Matches;

namespace TournamentAPI.UnitTests.Services;

public class MatchCascadePositionCalculatorTests
{
    [Fact]
    public void GetDownstreamMatchId_WhenMatchIsAtEvenPosition_ReturnsFirstNextRoundMatch()
    {
        var currentRound = new List<int> { 10, 11, 12, 13 };
        var nextRound = new List<int> { 20, 21 };

        var result = MatchCascadePositionCalculator.GetDownstreamMatchId(currentRound, nextRound, 10);

        Assert.Equal(20, result);
    }

    [Fact]
    public void GetDownstreamMatchId_WhenMatchIsAtOddPosition_ReturnsSameNextRoundMatchAsItsPair()
    {
        var currentRound = new List<int> { 10, 11, 12, 13 };
        var nextRound = new List<int> { 20, 21 };

        var result = MatchCascadePositionCalculator.GetDownstreamMatchId(currentRound, nextRound, 11);

        Assert.Equal(20, result);
    }

    [Fact]
    public void GetDownstreamMatchId_WhenNextRoundNotGenerated_ReturnsNull()
    {
        var currentRound = new List<int> { 10, 11 };
        var nextRound = new List<int>();

        var result = MatchCascadePositionCalculator.GetDownstreamMatchId(currentRound, nextRound, 10);

        Assert.Null(result);
    }

    [Fact]
    public void GetDownstreamMatchId_WhenMatchIdNotInCurrentRound_ReturnsNull()
    {
        var currentRound = new List<int> { 10, 11 };
        var nextRound = new List<int> { 20 };

        var result = MatchCascadePositionCalculator.GetDownstreamMatchId(currentRound, nextRound, 999);

        Assert.Null(result);
    }

    [Fact]
    public void GetDownstreamMatchId_WhenPositionIsTrailingByeNotYetGenerated_ReturnsNull()
    {
        var currentRound = new List<int> { 10, 11, 12 };
        var nextRound = new List<int> { 20 };

        var result = MatchCascadePositionCalculator.GetDownstreamMatchId(currentRound, nextRound, 12);

        Assert.Null(result);
    }
}
