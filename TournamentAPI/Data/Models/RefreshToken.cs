namespace TournamentAPI.Data.Models;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiryDateUtc { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
