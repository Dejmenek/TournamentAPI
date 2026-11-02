using Stripe;
using TournamentAPI.Data.Models;
using TournamentAPI.Payments;

namespace TournamentAPI.UnitTests.Payments;

public class PayoutAccountStatusMapperTests
{
    [Fact]
    public void FromAccount_ReturnsEnabled_WhenTransfersCapabilityIsActive()
    {
        // Arrange
        var account = CreateAccount(transfers: "active", detailsSubmitted: true);

        // Act
        var status = PayoutAccountStatusMapper.FromAccount(account);

        // Assert
        Assert.Equal(PayoutAccountStatus.Enabled, status);
    }

    [Fact]
    public void FromAccount_ReturnsEnabled_WhenTransfersAreActiveButMoreInformationIsDue()
    {
        // Arrange
        var account = CreateAccount(transfers: "active", detailsSubmitted: true, currentlyDue: ["individual.id_number"]);

        // Act
        var status = PayoutAccountStatusMapper.FromAccount(account);

        // Assert
        Assert.Equal(PayoutAccountStatus.Enabled, status);
    }

    [Fact]
    public void FromAccount_ReturnsPending_WhenOnboardingDetailsAreNotSubmitted()
    {
        // Arrange
        var account = CreateAccount(
            transfers: "inactive",
            detailsSubmitted: false,
            currentlyDue: ["individual.first_name"],
            pastDue: ["individual.first_name"],
            disabledReason: "requirements.past_due");

        // Act
        var status = PayoutAccountStatusMapper.FromAccount(account);

        // Assert
        Assert.Equal(PayoutAccountStatus.Pending, status);
    }

    [Fact]
    public void FromAccount_ReturnsPending_WhenTransfersCapabilityIsUnderReview()
    {
        // Arrange
        var account = CreateAccount(transfers: "pending", detailsSubmitted: true, disabledReason: "requirements.pending_verification");

        // Act
        var status = PayoutAccountStatusMapper.FromAccount(account);

        // Assert
        Assert.Equal(PayoutAccountStatus.Pending, status);
    }

    [Fact]
    public void FromAccount_ReturnsRestricted_WhenTransfersAreDisabledAfterOnboarding()
    {
        // Arrange
        var account = CreateAccount(
            transfers: "inactive",
            detailsSubmitted: true,
            pastDue: ["individual.verification.document"],
            disabledReason: "requirements.past_due");

        // Act
        var status = PayoutAccountStatusMapper.FromAccount(account);

        // Assert
        Assert.Equal(PayoutAccountStatus.Restricted, status);
    }

    [Fact]
    public void FromAccount_ReturnsRestricted_WhenTransfersCapabilityIsMissingAfterOnboarding()
    {
        // Arrange
        var account = new Account { Id = "acct_test", DetailsSubmitted = true };

        // Act
        var status = PayoutAccountStatusMapper.FromAccount(account);

        // Assert
        Assert.Equal(PayoutAccountStatus.Restricted, status);
    }

    private static Account CreateAccount(
        string transfers,
        bool detailsSubmitted,
        List<string>? currentlyDue = null,
        List<string>? pastDue = null,
        string? disabledReason = null) => new()
    {
        Id = "acct_test",
        DetailsSubmitted = detailsSubmitted,
        Capabilities = new AccountCapabilities { Transfers = transfers },
        Requirements = new AccountRequirements
        {
            CurrentlyDue = currentlyDue ?? [],
            PastDue = pastDue ?? [],
            DisabledReason = disabledReason
        }
    };
}
