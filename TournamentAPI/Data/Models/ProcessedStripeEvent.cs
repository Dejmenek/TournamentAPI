namespace TournamentAPI.Data.Models;

public class ProcessedStripeEvent
{
    public const string PrimaryKeyName = "PK_ProcessedStripeEvents";

    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public DateTime ProcessedAt { get; set; }
}
