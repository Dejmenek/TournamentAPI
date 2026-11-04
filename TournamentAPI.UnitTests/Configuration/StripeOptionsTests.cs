using System.ComponentModel.DataAnnotations;
using TournamentAPI.Configuration;

namespace TournamentAPI.UnitTests.Configuration;

public class StripeOptionsTests
{
    [Fact]
    public void Validation_Succeeds_WhenCurrencyIsEur()
    {
        // Arrange
        var options = CreateOptions("eur");

        // Act
        var results = Validate(options);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("EUR")]
    [InlineData("")]
    public void Validation_Fails_WhenCurrencyIsNotEur(string currency)
    {
        // Arrange
        var options = CreateOptions(currency);

        // Act
        var results = Validate(options);

        // Assert
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(StripeOptions.Currency)));
    }

    private static StripeOptions CreateOptions(string currency) => new()
    {
        SecretKey = "sk_test_123",
        WebhookSigningSecret = "whsec_test",
        Currency = currency,
        OnboardingRefreshUrl = "https://example.com/payouts/onboarding/refresh",
        OnboardingReturnUrl = "https://example.com/payouts/onboarding/return",
    };

    private static List<ValidationResult> Validate(StripeOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
