namespace TournamentAPI.Models;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public Bracket Bracket { get; set; } = null!;
    public ICollection<ApplicationUser> Participants { get; set; } = new List<ApplicationUser>();
}
