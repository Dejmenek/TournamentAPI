using Stripe;
using Stripe.Checkout;

namespace TournamentAPI.Payments;

public class StripeGateway : IStripeGateway
{
    public const string UserIdMetadataKey = "user_id";

    private readonly StripeClient _client;

    public StripeGateway(StripeClient client)
    {
        _client = client;
    }

    public Task<Session> CreateCheckoutSessionAsync(CheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        var options = new SessionCreateOptions
        {
            Mode = PrizePoolFunding.CheckoutModePayment,
            AllowedPaymentMethodTypes = [PrizePoolFunding.CardPaymentMethod],
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            ExpiresAt = request.ExpiresAt,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        UnitAmount = request.Amount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.ProductName }
                    }
                }
            ],
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                TransferGroup = request.TransferGroup,
                Metadata = request.Metadata.ToDictionary(m => m.Key, m => m.Value)
            },
            Metadata = request.Metadata.ToDictionary(m => m.Key, m => m.Value)
        };

        var requestOptions = new RequestOptions { IdempotencyKey = request.IdempotencyKey };

        return _client.V1.Checkout.Sessions.CreateAsync(options, requestOptions, cancellationToken);
    }

    public Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken)
        => _client.V1.PaymentIntents.GetAsync(paymentIntentId, cancellationToken: cancellationToken);

    public Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken)
        => _client.V1.Checkout.Sessions.GetAsync(sessionId, cancellationToken: cancellationToken);

    public Task<Account> GetAccountAsync(string accountId, CancellationToken cancellationToken)
        => _client.V1.Accounts.GetAsync(accountId, cancellationToken: cancellationToken);

    public Task<Account> CreateConnectedAccountAsync(ConnectedAccountRequest request, CancellationToken cancellationToken)
    {
        var options = new AccountCreateOptions
        {
            Country = request.Country,
            Email = request.Email,
            Controller = new AccountControllerOptions
            {
                StripeDashboard = new AccountControllerStripeDashboardOptions { Type = "express" },
                RequirementCollection = "stripe",
                Fees = new AccountControllerFeesOptions { Payer = "application" },
                Losses = new AccountControllerLossesOptions { Payments = "application" }
            },
            Capabilities = new AccountCapabilitiesOptions
            {
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
            },
            Metadata = new Dictionary<string, string>
            {
                [UserIdMetadataKey] = request.UserId.ToString()
            }
        };

        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"connected-account-user-{request.UserId}-{request.Attempt}"
        };

        return _client.V1.Accounts.CreateAsync(options, requestOptions, cancellationToken);
    }

    public Task<AccountLink> CreateOnboardingLinkAsync(OnboardingLinkRequest request, CancellationToken cancellationToken)
    {
        var options = new AccountLinkCreateOptions
        {
            Account = request.AccountId,
            RefreshUrl = request.RefreshUrl,
            ReturnUrl = request.ReturnUrl,
            Type = "account_onboarding"
        };

        return _client.V1.AccountLinks.CreateAsync(options, cancellationToken: cancellationToken);
    }
}
