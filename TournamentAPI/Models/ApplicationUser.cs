using Microsoft.AspNetCore.Identity;

namespace TournamentAPI.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public ICollection<TournamentParticipant> ParticipatedTournaments { get; set; } = new List<TournamentParticipant>();
    public ICollection<Tournament> OwnedTournaments { get; set; } = new List<Tournament>();
    public ICollection<Match> MatchesAsPlayer1 { get; set; } = new List<Match>();
    public ICollection<Match> MatchesAsPlayer2 { get; set; } = new List<Match>();
    public ICollection<Match> MatchesWon { get; set; } = new List<Match>();
}
