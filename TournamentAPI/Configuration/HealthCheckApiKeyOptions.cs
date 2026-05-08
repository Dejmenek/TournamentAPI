using System.ComponentModel.DataAnnotations;

namespace TournamentAPI.Configuration;

public record HealthCheckApiKeyOptions
{
    public const string SectionName = "HealthCheck";

    [Required]
    public string ApiKey { get; init; } = string.Empty;
}
