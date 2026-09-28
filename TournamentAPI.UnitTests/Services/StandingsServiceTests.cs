using TournamentAPI.Data.Models;
using TournamentAPI.Standings;

namespace TournamentAPI.UnitTests.Services;

public class StandingsServiceTests
{
    private readonly StandingsService _sut = new();

    [Fact]
    public void ComputeStandings_ComputesPointsAndCountsCorrectly()
    {
        var matches = new List<Match>
        {
            new() { Player1Id = 1, Player2Id = 2, WinnerId = 1, Status = MatchStatus.Played },
            new() { Player1Id = 1, Player2Id = 3, WinnerId = null, Status = MatchStatus.Drawn },
            new() { Player1Id = 2, Player2Id = 3, WinnerId = null, Status = MatchStatus.Scheduled }
        };

        var standings = _sut.ComputeStandings(matches);

        var p1 = standings.Single(s => s.ParticipantId == 1);
        var p2 = standings.Single(s => s.ParticipantId == 2);
        var p3 = standings.Single(s => s.ParticipantId == 3);

        Assert.Equal(1, p1.Wins);
        Assert.Equal(1, p1.Draws);
        Assert.Equal(0, p1.Losses);
        Assert.Equal(4, p1.Points);
        Assert.Equal(2, p1.MatchesPlayed);
        Assert.Equal(0, p1.MatchesRemaining);

        Assert.Equal(0, p2.Wins);
        Assert.Equal(0, p2.Draws);
        Assert.Equal(1, p2.Losses);
        Assert.Equal(0, p2.Points);
        Assert.Equal(1, p2.MatchesPlayed);
        Assert.Equal(1, p2.MatchesRemaining);

        Assert.Equal(0, p3.Wins);
        Assert.Equal(1, p3.Draws);
        Assert.Equal(0, p3.Losses);
        Assert.Equal(1, p3.Points);
        Assert.Equal(1, p3.MatchesPlayed);
        Assert.Equal(1, p3.MatchesRemaining);

        Assert.Equal(1, p1.Rank);
        Assert.Equal(2, p3.Rank);
        Assert.Equal(3, p2.Rank);
    }

    [Fact]
    public void ComputeStandings_WhenTwoParticipantsTieOnPoints_BreaksTieByHeadToHeadResult()
    {
        // P1 and P2 both finish on 3 points; P1 beat P2 directly, so P1 should rank above P2.
        var matches = new List<Match>
        {
            new() { Player1Id = 1, Player2Id = 2, WinnerId = 1, Status = MatchStatus.Played },
            new() { Player1Id = 1, Player2Id = 4, WinnerId = 4, Status = MatchStatus.Played },
            new() { Player1Id = 2, Player2Id = 4, WinnerId = 2, Status = MatchStatus.Played },
            new() { Player1Id = 3, Player2Id = 4, WinnerId = 4, Status = MatchStatus.Played }
        };

        var standings = _sut.ComputeStandings(matches);

        var p1 = standings.Single(s => s.ParticipantId == 1);
        var p2 = standings.Single(s => s.ParticipantId == 2);
        var p3 = standings.Single(s => s.ParticipantId == 3);
        var p4 = standings.Single(s => s.ParticipantId == 4);

        Assert.Equal(3, p1.Points);
        Assert.Equal(3, p2.Points);

        Assert.Equal(1, p4.Rank);
        Assert.Equal(2, p1.Rank);
        Assert.Equal(3, p2.Rank);
        Assert.Equal(4, p3.Rank);
    }

    [Fact]
    public void ComputeStandings_WhenHeadToHeadCannotResolveTie_ParticipantsShareRank()
    {
        // P1 and P2 both finish on 4 points and drew their head-to-head match, so neither outranks the other.
        var matches = new List<Match>
        {
            new() { Player1Id = 1, Player2Id = 2, WinnerId = null, Status = MatchStatus.Drawn },
            new() { Player1Id = 1, Player2Id = 3, WinnerId = 1, Status = MatchStatus.Played },
            new() { Player1Id = 2, Player2Id = 3, WinnerId = 2, Status = MatchStatus.Played }
        };

        var standings = _sut.ComputeStandings(matches);

        var p1 = standings.Single(s => s.ParticipantId == 1);
        var p2 = standings.Single(s => s.ParticipantId == 2);
        var p3 = standings.Single(s => s.ParticipantId == 3);

        Assert.Equal(4, p1.Points);
        Assert.Equal(4, p2.Points);
        Assert.Equal(1, p1.Rank);
        Assert.Equal(1, p2.Rank);
        Assert.Equal(3, p3.Rank);
    }

    [Fact]
    public void ComputeStandings_ByeMatch_CountsAsAWinAndAsAMatchPlayed()
    {
        var matches = new List<Match>
        {
            new() { Player1Id = 1, Player2Id = null, WinnerId = 1, Status = MatchStatus.Played }
        };

        var standings = _sut.ComputeStandings(matches);

        var p1 = standings.Single(s => s.ParticipantId == 1);

        Assert.Equal(1, p1.Wins);
        Assert.Equal(0, p1.Losses);
        Assert.Equal(3, p1.Points);
        Assert.Equal(1, p1.MatchesPlayed);
    }
}
