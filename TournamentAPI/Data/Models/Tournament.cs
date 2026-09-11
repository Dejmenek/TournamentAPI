namespace TournamentAPI.Data.Models;

public class Tournament : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [IsProjected(true)]
    public DateTime StartDate { get; set; }
    [IsProjected(true)]
    public TournamentStatus Status { get; set; }
    public int OwnerId { get; set; }
    public int MaxParticipants { get; set; }

    [GraphQLIgnore]
    public bool IsDeleted { get; set; }

    [GraphQLIgnore]
    public Bracket? Bracket { get; set; }
    [GraphQLIgnore]
    public ApplicationUser Owner { get; set; } = null!;
    [GraphQLIgnore]
    public ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();

    [GraphQLIgnore]
    public bool IsActive(DateTime utcNow) => Status == TournamentStatus.Open && StartDate > utcNow;
}
