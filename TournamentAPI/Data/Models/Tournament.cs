namespace TournamentAPI.Data.Models;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public TournamentStatus Status { get; set; }
    public int OwnerId { get; set; }
    public bool IsDeleted { get; set; }

    public Bracket? Bracket { get; set; }
    public ApplicationUser Owner { get; set; } = null!;
    public ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
}
