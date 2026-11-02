using Stripe;
using Stripe.Checkout;

namespace TournamentAPI.Payments;

public sealed record ConnectedAccountRequest(int UserId, string Country, string? Email, int Attempt);

public sealed record OnboardingLinkRequest(string AccountId, string RefreshUrl, string ReturnUrl);

public interface IStripeGateway
{
    Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken);

    Task<Account> CreateConnectedAccountAsync(ConnectedAccountRequest request, CancellationToken cancellationToken);

    Task<AccountLink> CreateOnboardingLinkAsync(OnboardingLinkRequest request, CancellationToken cancellationToken);
}
