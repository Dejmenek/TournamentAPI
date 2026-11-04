namespace TournamentAPI.Data.Models;

public class TournamentParticipant : ISoftDeletable
{
    public const string SlotNumberUniqueIndexName = "IX_TournamentParticipants_TournamentId_SlotNumber";

    public int TournamentId { get; set; }
    [GraphQLIgnore]
    public Tournament Tournament { get; set; } = null!;

    public int ParticipantId { get; set; }
    [GraphQLIgnore]
    public ApplicationUser Participant { get; set; } = null!;
    public int SlotNumber { get; set; }
    public EntryStatus EntryStatus { get; set; } = EntryStatus.Confirmed;
    [GraphQLIgnore]
    public bool IsDeleted { get; set; }
}
