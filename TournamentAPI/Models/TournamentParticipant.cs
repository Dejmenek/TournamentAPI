namespace TournamentAPI.Models;

public class TournamentParticipant
{
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public int ParticipantId { get; set; }
    public ApplicationUser Participant { get; set; } = null!;
    public bool IsDeleted { get; set; }
}
