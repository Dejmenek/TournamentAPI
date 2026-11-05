using System.ComponentModel.DataAnnotations;

namespace TournamentAPI.Configuration;

public record PaidTournamentOptions
{
    public const string SectionName = "PaidTournaments";

    [Range(0, 100)]
    public decimal PlatformFeePercent { get; init; } = 5m;

    [Required]
    [Url]
    public string FundingSuccessUrl { get; init; } = string.Empty;

    [Required]
    [Url]
    public string FundingCancelUrl { get; init; } = string.Empty;

    [Range(30, 1440)]
    public int CheckoutExpiryMinutes { get; init; } = 30;
}
