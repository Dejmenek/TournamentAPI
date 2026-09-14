using System.ComponentModel.DataAnnotations;

namespace TournamentAPI.Data.Models;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = null!;
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Revoked { get; set; }
    public string? ReplacedByToken { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    [Timestamp]
    public byte[] Version { get; set; } = null!;

    public bool IsActive => Revoked is null && DateTime.UtcNow < Expires;
}
