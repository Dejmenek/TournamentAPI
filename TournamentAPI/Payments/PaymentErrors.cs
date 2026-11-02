namespace TournamentAPI.Payments;

public static class PaymentErrors
{
    public static IError UnsupportedPayoutCountry(string country) =>
        ErrorBuilder.New()
            .SetMessage("Payout accounts can only be created in an EU or EEA country.")
            .SetCode(PaymentErrorCodes.UnsupportedPayoutCountry)
            .SetExtension("Country", country)
            .Build();

    public static IError StripeRequestFailed() =>
        ErrorBuilder.New()
            .SetMessage("The payment provider could not complete the request. Please try again later.")
            .SetCode(PaymentErrorCodes.StripeRequestFailed)
            .Build();

    public static IError PayoutAccountSaveFailed() =>
        ErrorBuilder.New()
            .SetMessage("The payout account was created but could not be saved. Please try again.")
            .SetCode(PaymentErrorCodes.PayoutAccountSaveFailed)
            .Build();
}
