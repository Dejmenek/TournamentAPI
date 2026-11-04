namespace TournamentAPI.Payments;

public static class PaymentErrorCodes
{
    public const string UnsupportedPayoutCountry = "Payment.UnsupportedPayoutCountry";
    public const string StripeRequestFailed = "Payment.StripeRequestFailed";
    public const string PayoutAccountSaveFailed = "Payment.PayoutAccountSaveFailed";
}
