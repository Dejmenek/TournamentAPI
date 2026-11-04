using TournamentAPI.Payments;

namespace TournamentAPI.UnitTests.Payments;

public class PaidTournamentFeeCalculatorTests
{
    [Theory]
    [InlineData(1000, 5, 50)]
    [InlineData(1001, 5, 51)]
    [InlineData(1, 5, 1)]
    [InlineData(333, 10, 34)]
    [InlineData(1000, 0, 0)]
    [InlineData(0, 5, 0)]
    [InlineData(10000, 0.7, 70)]
    [InlineData(10000, 0.07, 7)]
    [InlineData(100, 7.35, 8)]
    [InlineData(2000, 0.35, 7)]
    [InlineData(9000000000000000000, 100, 9000000000000000000)]
    public void CalculatePlatformFee_RoundsUpToWholeCents(long amount, decimal percent, long expected)
    {
        // Act
        var fee = PaidTournamentFeeCalculator.CalculatePlatformFee(amount, percent);

        // Assert
        Assert.Equal(expected, fee);
    }

    [Fact]
    public void CalculateEscrowAmount_AddsPlatformFeeOnPrizeTotal()
    {
        // Arrange
        long[] prizes = [5000, 2500, 1001];

        // Act
        var escrow = PaidTournamentFeeCalculator.CalculateEscrowAmount(prizes, 5m);

        // Assert
        Assert.Equal(8501 + 426, escrow);
    }

    [Fact]
    public void CalculateEscrowAmount_WithNoPrizes_ReturnsZero()
    {
        // Act
        var escrow = PaidTournamentFeeCalculator.CalculateEscrowAmount([], 5m);

        // Assert
        Assert.Equal(0, escrow);
    }

    [Fact]
    public void SplitTiedPrizes_WithSingleUser_GetsTheirPlacementPrize()
    {
        // Arrange
        var prizes = new Dictionary<int, long> { [1] = 5000, [2] = 2000 };

        // Act
        var split = PaidTournamentFeeCalculator.SplitTiedPrizes(prizes, firstPlacement: 2, userIds: [7]);

        // Assert
        Assert.Equal(2000, split[7]);
    }

    [Fact]
    public void SplitTiedPrizes_SplitsCombinedPrizesOfCoveredPlacementsEqually()
    {
        // Arrange
        var prizes = new Dictionary<int, long> { [1] = 5000, [2] = 3000, [3] = 1000 };

        // Act
        var split = PaidTournamentFeeCalculator.SplitTiedPrizes(prizes, firstPlacement: 2, userIds: [4, 9]);

        // Assert
        Assert.Equal(2000, split[4]);
        Assert.Equal(2000, split[9]);
    }

    [Fact]
    public void SplitTiedPrizes_GivesLeftoverCentsToLowestUserIdsFirst()
    {
        // Arrange
        var prizes = new Dictionary<int, long> { [1] = 100 };

        // Act
        var split = PaidTournamentFeeCalculator.SplitTiedPrizes(prizes, firstPlacement: 1, userIds: [9, 3, 5]);

        // Assert
        Assert.Equal(34, split[3]);
        Assert.Equal(33, split[5]);
        Assert.Equal(33, split[9]);
        Assert.Equal(100, split.Values.Sum());
    }

    [Fact]
    public void SplitTiedPrizes_TreatsPlacementsWithoutPrizeAsZero()
    {
        // Arrange
        var prizes = new Dictionary<int, long> { [1] = 5000, [2] = 1001 };

        // Act
        var split = PaidTournamentFeeCalculator.SplitTiedPrizes(prizes, firstPlacement: 2, userIds: [1, 2, 3]);

        // Assert
        Assert.Equal(334, split[1]);
        Assert.Equal(334, split[2]);
        Assert.Equal(333, split[3]);
    }

    [Fact]
    public void SplitTiedPrizes_WhenTiedPlacementsHaveNoPrize_PaysNothing()
    {
        // Arrange
        var prizes = new Dictionary<int, long> { [1] = 5000 };

        // Act
        var split = PaidTournamentFeeCalculator.SplitTiedPrizes(prizes, firstPlacement: 3, userIds: [1, 2]);

        // Assert
        Assert.All(split.Values, amount => Assert.Equal(0, amount));
    }
}
