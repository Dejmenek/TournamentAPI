using TournamentAPI.Brackets;
using TournamentAPI.Data.Models;

namespace TournamentAPI.UnitTests.Services;

public class RoundRobinBracketStrategyTests
{
    private readonly RoundRobinBracketStrategy _sut = new();

    [Fact]
    public void CreateBracket_SetsTournamentId()
    {
        var bracket = _sut.CreateBracket(tournamentId: 42, participantIds: [1, 2]);

        Assert.Equal(42, bracket.TournamentId);
    }

    [Fact]
    public void CreateBracket_WithEvenParticipantCount_CreatesCorrectNumberOfMatches()
    {
        var participantIds = new List<int> { 1, 2, 3, 4, 5, 6 };
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: participantIds);

        Assert.Equal(participantIds.Count * (participantIds.Count - 1) / 2, bracket.Matches.Count);
    }

    [Fact]
    public void CreateBracket_WithEvenParticipantCount_EveryPairAppearsExactlyOnce()
    {
        var participantIds = new List<int> { 1, 2, 3, 4, 5, 6 };
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: participantIds);

        var pairs = bracket.Matches
            .Select(m => (Math.Min(m.Player1Id, m.Player2Id!.Value), Math.Max(m.Player1Id, m.Player2Id!.Value)))
            .ToList();

        Assert.Equal(pairs.Count, pairs.Distinct().Count());

        var expectedPairs =
            from a in participantIds
            from b in participantIds
            where a < b
            select (a, b);

        Assert.Equal(expectedPairs.OrderBy(p => p).ToList(), pairs.OrderBy(p => p).ToList());
    }

    [Fact]
    public void CreateBracket_WithEvenParticipantCount_HasNoByes()
    {
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: [1, 2, 3, 4, 5, 6]);

        Assert.DoesNotContain(bracket.Matches, m => m.Player2Id == null);
    }

    [Fact]
    public void CreateBracket_WithOddParticipantCount_CreatesCorrectNumberOfMatches()
    {
        var participantIds = new List<int> { 1, 2, 3, 4, 5 };
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: participantIds);

        Assert.Equal(participantIds.Count * (participantIds.Count - 1) / 2, bracket.Matches.Count);
    }

    [Fact]
    public void CreateBracket_WithOddParticipantCount_EveryPairAppearsExactlyOnce()
    {
        var participantIds = new List<int> { 1, 2, 3, 4, 5 };
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: participantIds);

        var pairs = bracket.Matches
            .Select(m => (Math.Min(m.Player1Id, m.Player2Id!.Value), Math.Max(m.Player1Id, m.Player2Id!.Value)))
            .ToList();

        Assert.Equal(pairs.Count, pairs.Distinct().Count());

        var expectedPairs =
            from a in participantIds
            from b in participantIds
            where a < b
            select (a, b);

        Assert.Equal(expectedPairs.OrderBy(p => p).ToList(), pairs.OrderBy(p => p).ToList());
    }

    [Fact]
    public void CreateBracket_WithOddParticipantCount_HasNoByeRows()
    {
        // Byes aren't persisted as Match rows: a participant simply has no match in their bye round.
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: [1, 2, 3, 4, 5]);

        Assert.DoesNotContain(bracket.Matches, m => m.Player2Id == null);
    }

    [Fact]
    public void CreateBracket_WithOddParticipantCount_EveryParticipantIsMissingFromExactlyOneRound()
    {
        var participantIds = new List<int> { 1, 2, 3, 4, 5 };
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: participantIds);

        var rounds = bracket.Matches.Select(m => m.Round).Distinct().ToList();
        Assert.Equal(participantIds.Count, rounds.Count);

        foreach (var participantId in participantIds)
        {
            var roundsPlayed = bracket.Matches
                .Count(m => m.Player1Id == participantId || m.Player2Id == participantId);

            Assert.Equal(participantIds.Count - 1, roundsPlayed);
        }
    }

    [Fact]
    public void CreateBracket_ScheduledMatchesHaveNoWinner()
    {
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: [1, 2, 3, 4]);

        Assert.All(bracket.Matches, m =>
        {
            Assert.Null(m.WinnerId);
            Assert.Equal(MatchStatus.Scheduled, m.Status);
        });
    }

    [Fact]
    public void CreateBracket_SetsMatchBracketReference()
    {
        var bracket = _sut.CreateBracket(tournamentId: 1, participantIds: [1, 2]);

        Assert.All(bracket.Matches, m => Assert.Same(bracket, m.Bracket));
    }
}
