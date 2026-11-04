namespace TournamentAPI.Data.Models;

public class TournamentPrize
{
    public int TournamentId { get; set; }
    public int Placement { get; set; }
    public long Amount { get; set; }
}
