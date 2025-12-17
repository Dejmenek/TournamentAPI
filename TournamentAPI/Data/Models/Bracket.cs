namespace TournamentAPI.Data.Models;

public class Bracket
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public bool IsDeleted { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
