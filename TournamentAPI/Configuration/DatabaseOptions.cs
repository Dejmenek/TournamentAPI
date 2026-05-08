using System.ComponentModel.DataAnnotations;

namespace TournamentAPI.Configuration;

public record DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string DefaultConnection { get; init; } = string.Empty;
}
