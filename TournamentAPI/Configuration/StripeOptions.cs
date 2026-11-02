using System.ComponentModel.DataAnnotations;

namespace TournamentAPI.Configuration;

public record StripeOptions
{
    public const string SectionName = "Stripe";

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string WebhookSigningSecret { get; init; } = string.Empty;

    [Required]
    [AllowedValues("eur")]
    public string Currency { get; init; } = "eur";
}
