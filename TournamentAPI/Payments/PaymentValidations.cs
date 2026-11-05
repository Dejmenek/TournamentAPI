using TournamentAPI.Data.Models;

namespace TournamentAPI.Payments;

public static class PaymentValidations
{
    private static readonly HashSet<string> PayoutCountries = new(StringComparer.Ordinal)
    {
        "AT", "BE", "BG", "CY", "CZ", "DE", "DK", "EE", "ES", "FI", "FR", "GR", "HR", "HU",
        "IE", "IT", "LT", "LU", "LV", "MT", "NL", "PL", "PT", "RO", "SE", "SI", "SK",
        "IS", "LI", "NO"
    };

    public static IError? ValidatePayoutCountry(string country)
        => PayoutCountries.Contains(country) ? null : PaymentErrors.UnsupportedPayoutCountry(country);

    public static IError? ValidateTournamentDoesNotRequireCheckout(int tournamentId, long entryFeeAmount)
        => entryFeeAmount > 0 ? PaymentErrors.PaidTournamentRequiresCheckout(tournamentId) : null;

    public static IError? ValidatePrizePoolFundable(Tournament tournament, DateTime now)
        => tournament.Status == TournamentStatus.Closed
           && tournament.StartDate > now
           && tournament.PrizePoolStatus == PrizePoolStatus.Unfunded
            ? null
            : PaymentErrors.PrizePoolNotFundable(tournament.Id);

    public static IError? ValidatePayoutAccountEnabledForPaidTournament(int ownerId, long entryFeeAmount, PayoutAccountStatus payoutAccountStatus)
        => entryFeeAmount > 0 && payoutAccountStatus != PayoutAccountStatus.Enabled ? PaymentErrors.PayoutAccountNotEnabled(ownerId) : null;
}
