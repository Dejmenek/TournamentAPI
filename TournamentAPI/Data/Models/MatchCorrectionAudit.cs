namespace TournamentAPI.Data.Models;

public class MatchCorrectionAudit
{
    public Guid Id { get; set; }
    public int MatchId { get; set; }
    public Guid CorrelationId { get; set; }
    public int? TriggeredByMatchId { get; set; }

    public MatchStatus PreviousStatus { get; set; }
    public MatchStatus NewStatus { get; set; }

    public int? PreviousWinnerId { get; set; }
    public int? NewWinnerId { get; set; }

    public int PreviousPlayer1Id { get; set; }
    public int NewPlayer1Id { get; set; }
    public int? PreviousPlayer2Id { get; set; }
    public int? NewPlayer2Id { get; set; }

    public int PreviousPlayer1Score { get; set; }
    public int NewPlayer1Score { get; set; }
    public int PreviousPlayer2Score { get; set; }
    public int NewPlayer2Score { get; set; }

    public int PerformedByUserId { get; set; }
    public DateTime PerformedAtUtc { get; set; }
    public string? Notes { get; set; }
}
