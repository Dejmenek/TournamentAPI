using System.ComponentModel.DataAnnotations;

namespace TournamentAPI.Configuration;

public record PaidTournamentOptions
{
    public const string SectionName = "PaidTournaments";

    [Range(0, 100)]
    public decimal PlatformFeePercent { get; init; } = 5m;
}
