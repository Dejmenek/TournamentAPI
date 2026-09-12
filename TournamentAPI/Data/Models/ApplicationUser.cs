using Microsoft.AspNetCore.Identity;

namespace TournamentAPI.Data.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    [IsProjected(true)]
    public bool IsEmailPublic { get; set; } = false;

    [GraphQLIgnore]
    public ICollection<TournamentParticipant> ParticipatedTournaments { get; set; } = new List<TournamentParticipant>();
    [GraphQLIgnore]
    public ICollection<Tournament> OwnedTournaments { get; set; } = new List<Tournament>();
    [GraphQLIgnore]
    public ICollection<Match> MatchesAsPlayer1 { get; set; } = new List<Match>();
    [GraphQLIgnore]
    public ICollection<Match> MatchesAsPlayer2 { get; set; } = new List<Match>();
    [GraphQLIgnore]
    public ICollection<Match> MatchesWon { get; set; } = new List<Match>();
}
