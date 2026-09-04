namespace TournamentAPI.Data.Models;

public class TournamentParticipant
{
    public const string SlotNumberUniqueIndexName = "IX_TournamentParticipants_TournamentId_SlotNumber";

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public int ParticipantId { get; set; }
    public ApplicationUser Participant { get; set; } = null!;
    public int SlotNumber { get; set; }
    public bool IsDeleted { get; set; }
}
