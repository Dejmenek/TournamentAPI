using Stripe;
using Stripe.Checkout;

namespace TournamentAPI.Payments;

public sealed record ConnectedAccountRequest(int UserId, string Country, string? Email, int Attempt);

public sealed record OnboardingLinkRequest(string AccountId, string RefreshUrl, string ReturnUrl);

public sealed record CheckoutSessionRequest(
    string IdempotencyKey,
    long Amount,
    string Currency,
    string ProductName,
    string SuccessUrl,
    string CancelUrl,
    string TransferGroup,
    IReadOnlyDictionary<string, string> Metadata,
    DateTime ExpiresAt);

public interface IStripeGateway
{
    Task<Session> CreateCheckoutSessionAsync(CheckoutSessionRequest request, CancellationToken cancellationToken);

    Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken);

    Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken);

    Task<Account> GetAccountAsync(string accountId, CancellationToken cancellationToken);

    Task<Account> CreateConnectedAccountAsync(ConnectedAccountRequest request, CancellationToken cancellationToken);

    Task<AccountLink> CreateOnboardingLinkAsync(OnboardingLinkRequest request, CancellationToken cancellationToken);
}
