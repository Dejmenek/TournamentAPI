using System.ComponentModel.DataAnnotations;

namespace TournamentAPI.Configuration;

public record JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;
}
