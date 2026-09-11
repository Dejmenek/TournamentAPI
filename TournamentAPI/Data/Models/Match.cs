using System.ComponentModel.DataAnnotations;
using HotChocolate;

namespace TournamentAPI.Data.Models;

public class Match : ISoftDeletable
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int BracketId { get; set; }
    public int Player1Id { get; set; }
    public int? Player2Id { get; set; }
    public int? WinnerId { get; set; }

    [GraphQLIgnore]
    public bool IsDeleted { get; set; }

    [GraphQLIgnore]
    public ApplicationUser Player1 { get; set; } = null!;

    [GraphQLIgnore]
    public ApplicationUser? Player2 { get; set; }

    [GraphQLIgnore]
    public ApplicationUser? Winner { get; set; }

    [GraphQLIgnore]
    public Bracket Bracket { get; set; } = null!;

    [Timestamp]
    [GraphQLIgnore]
    public byte[] Version { get; set; } = null!;
}
