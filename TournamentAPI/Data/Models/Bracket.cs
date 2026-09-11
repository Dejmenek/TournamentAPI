using HotChocolate;

namespace TournamentAPI.Data.Models;

public class Bracket : ISoftDeletable
{
    public int Id { get; set; }
    public int TournamentId { get; set; }

    [GraphQLIgnore]
    public bool IsDeleted { get; set; }

    [GraphQLIgnore]
    public Tournament Tournament { get; set; } = null!;

    [GraphQLIgnore]
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
