namespace TournamentAPI.Data.Models;

public class Match
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int BracketId { get; set; }
    public int Player1Id { get; set; }
    public int? Player2Id { get; set; }
    public int? WinnerId { get; set; }
    public bool IsDeleted { get; set; }

    public ApplicationUser Player1 { get; set; } = null!;
    public ApplicationUser? Player2 { get; set; }
    public ApplicationUser? Winner { get; set; }
    public Bracket Bracket { get; set; } = null!;
}
