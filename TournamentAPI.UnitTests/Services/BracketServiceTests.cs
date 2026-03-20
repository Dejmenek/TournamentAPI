using TournamentAPI.Brackets;

namespace TournamentAPI.UnitTests.Services;

public class BracketServiceTests
{
    [Fact]
    public void CreateBracket_SetsTournamentId()
    {
        var bracket = BracketService.CreateBracket(tournamentId: 42, participantIds: [1, 2]);

        Assert.Equal(42, bracket.TournamentId);
    }

    [Fact]
    public void CreateBracket_WithEvenParticipantCount_CreatesCorrectNumberOfMatches()
    {
        var bracket = BracketService.CreateBracket(tournamentId: 1, participantIds: [1, 2, 3, 4]);

        Assert.Equal(2, bracket.Matches.Count);
    }

    [Fact]
    public void CreateBracket_WithOddParticipantCount_CreatesCorrectNumberOfMatches()
    {
        var bracket = BracketService.CreateBracket(tournamentId: 1, participantIds: [1, 2, 3]);

        Assert.Equal(2, bracket.Matches.Count);
    }

    [Fact]
    public void CreateBracket_WithOddParticipantCount_LastMatchHasNullPlayer2()
    {
        var bracket = BracketService.CreateBracket(tournamentId: 1, participantIds: [1, 2, 3]);

        var byeMatch = bracket.Matches.SingleOrDefault(m => m.Player2Id == null);
        Assert.NotNull(byeMatch);
    }

    [Fact]
    public void CreateBracket_AllMatchesAreInRoundOne()
    {
        var bracket = BracketService.CreateBracket(tournamentId: 1, participantIds: [1, 2, 3, 4]);

        Assert.All(bracket.Matches, m => Assert.Equal(1, m.Round));
    }

    [Fact]
    public void CreateBracket_AllParticipantIdsAppearInMatches()
    {
        var participantIds = new List<int> { 1, 2, 3, 4 };
        var bracket = BracketService.CreateBracket(tournamentId: 1, participantIds: participantIds);

        var usedIds = bracket.Matches
            .SelectMany(m => new[] { m.Player1Id, m.Player2Id ?? -1 })
            .Where(id => id != -1)
            .ToHashSet();

        Assert.Equal(participantIds.ToHashSet(), usedIds);
    }

    [Fact]
    public void CreateBracket_SetsMatchBracketReference()
    {
        var bracket = BracketService.CreateBracket(tournamentId: 1, participantIds: [1, 2]);

        Assert.All(bracket.Matches, m => Assert.Same(bracket, m.Bracket));
    }

    [Fact]
    public void CreateNextRoundMatches_WithEvenWinnerCount_CreatesCorrectNumberOfMatches()
    {
        var matches = BracketService.CreateNextRoundMatches(bracketId: 1, roundNumber: 1, winners: [1, 2, 3, 4]);

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void CreateNextRoundMatches_WithOddWinnerCount_LastMatchHasNullPlayer2()
    {
        var matches = BracketService.CreateNextRoundMatches(bracketId: 1, roundNumber: 1, winners: [1, 2, 3]);

        var byeMatch = matches.SingleOrDefault(m => m.Player2Id == null);
        Assert.NotNull(byeMatch);
    }

    [Fact]
    public void CreateNextRoundMatches_SetsCorrectRoundNumber()
    {
        var matches = BracketService.CreateNextRoundMatches(bracketId: 1, roundNumber: 2, winners: [1, 2]);

        Assert.All(matches, m => Assert.Equal(3, m.Round));
    }

    [Fact]
    public void CreateNextRoundMatches_SetsBracketId()
    {
        var matches = BracketService.CreateNextRoundMatches(bracketId: 7, roundNumber: 1, winners: [1, 2]);

        Assert.All(matches, m => Assert.Equal(7, m.BracketId));
    }

    [Fact]
    public void CreateNextRoundMatches_NormalizesPlayerOrder_LowerIdIsPlayer1()
    {
        // Winners [3, 1] — higher ID first, so after normalization Player1Id should be 1
        var matches = BracketService.CreateNextRoundMatches(bracketId: 1, roundNumber: 1, winners: [3, 1]);

        var match = matches.Single();
        Assert.Equal(1, match.Player1Id);
        Assert.Equal(3, match.Player2Id);
    }

    [Fact]
    public void CreateNextRoundMatches_WhenPlayerOrderIsAlreadyNormalized_DoesNotSwap()
    {
        var matches = BracketService.CreateNextRoundMatches(bracketId: 1, roundNumber: 1, winners: [1, 3]);

        var match = matches.Single();
        Assert.Equal(1, match.Player1Id);
        Assert.Equal(3, match.Player2Id);
    }
}
