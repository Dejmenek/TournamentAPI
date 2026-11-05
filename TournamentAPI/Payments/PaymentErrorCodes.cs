namespace TournamentAPI.Payments;

public static class PaymentErrorCodes
{
    public const string UnsupportedPayoutCountry = "Payment.UnsupportedPayoutCountry";
    public const string StripeRequestFailed = "Payment.StripeRequestFailed";
    public const string PayoutAccountSaveFailed = "Payment.PayoutAccountSaveFailed";
    public const string PaidTournamentRequiresCheckout = "Payment.PaidTournamentRequiresCheckout";
    public const string PayoutAccountNotEnabled = "Payment.PayoutAccountNotEnabled";
    public const string PrizePoolNotFundable = "Payment.PrizePoolNotFundable";
}
