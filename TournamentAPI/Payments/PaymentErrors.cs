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

    public static IError PaidTournamentRequiresCheckout(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("This tournament has an entry fee, so entries must be paid through checkout.")
            .SetCode(PaymentErrorCodes.PaidTournamentRequiresCheckout)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError PrizePoolNotFundable(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("The prize pool can only be funded while the tournament is closed, has not started, and its pool is unfunded.")
            .SetCode(PaymentErrorCodes.PrizePoolNotFundable)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError PayoutAccountNotEnabled(int ownerId) =>
        ErrorBuilder.New()
            .SetMessage("The tournament owner's payout account must be enabled before a tournament with an entry fee can open.")
            .SetCode(PaymentErrorCodes.PayoutAccountNotEnabled)
            .SetExtension("OwnerId", ownerId)
            .Build();
}
