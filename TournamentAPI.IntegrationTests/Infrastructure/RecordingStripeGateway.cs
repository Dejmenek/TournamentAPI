using System.Collections.Concurrent;
using Stripe;
using Stripe.Checkout;
using TournamentAPI.Payments;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed record StripeGatewayCall(string Method, object Arguments);

public sealed class RecordingStripeGateway : IStripeGateway
{
    private readonly ConcurrentQueue<StripeGatewayCall> _calls = new();
    private int _accountLinkCounter;

    public IReadOnlyList<StripeGatewayCall> Calls => _calls.ToArray();

    public Func<CheckoutSessionRequest, Session> CreateCheckoutSessionResult { get; set; } = DefaultCreatedCheckoutSession;

    public Func<string, PaymentIntent> PaymentIntentResult { get; set; } = DefaultPaymentIntent;

    public Func<string, Session> CheckoutSessionResult { get; set; } = DefaultCheckoutSession;

    public Func<string, Account> AccountResult { get; set; } = DefaultAccount;

    public Func<ConnectedAccountRequest, Account> ConnectedAccountResult { get; set; } = DefaultConnectedAccount;

    public Func<OnboardingLinkRequest, AccountLink>? OnboardingLinkResult { get; set; }

    public Task<Session> CreateCheckoutSessionAsync(CheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new StripeGatewayCall(nameof(CreateCheckoutSessionAsync), request));
        return Task.FromResult(CreateCheckoutSessionResult(request));
    }

    public Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new StripeGatewayCall(nameof(GetPaymentIntentAsync), paymentIntentId));
        return Task.FromResult(PaymentIntentResult(paymentIntentId));
    }

    public Task<Session> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new StripeGatewayCall(nameof(GetCheckoutSessionAsync), sessionId));
        return Task.FromResult(CheckoutSessionResult(sessionId));
    }

    public Task<Account> GetAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new StripeGatewayCall(nameof(GetAccountAsync), accountId));
        return Task.FromResult(AccountResult(accountId));
    }

    public Task<Account> CreateConnectedAccountAsync(ConnectedAccountRequest request, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new StripeGatewayCall(nameof(CreateConnectedAccountAsync), request));
        return Task.FromResult(ConnectedAccountResult(request));
    }

    public Task<AccountLink> CreateOnboardingLinkAsync(OnboardingLinkRequest request, CancellationToken cancellationToken)
    {
        _calls.Enqueue(new StripeGatewayCall(nameof(CreateOnboardingLinkAsync), request));

        if (OnboardingLinkResult is not null)
            return Task.FromResult(OnboardingLinkResult(request));

        var linkNumber = Interlocked.Increment(ref _accountLinkCounter);

        return Task.FromResult(new AccountLink
        {
            Object = "account_link",
            Url = $"https://connect.stripe.com/setup/e/{request.AccountId}/link_{linkNumber}",
            Created = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
    }

    public IReadOnlyList<T> CallsTo<T>(string method) =>
        Calls.Where(c => c.Method == method).Select(c => (T)c.Arguments).ToArray();

    public void Reset()
    {
        _calls.Clear();
        Interlocked.Exchange(ref _accountLinkCounter, 0);
        CheckoutSessionResult = DefaultCheckoutSession;
        CreateCheckoutSessionResult = DefaultCreatedCheckoutSession;
        PaymentIntentResult = DefaultPaymentIntent;
        AccountResult = DefaultAccount;
        ConnectedAccountResult = DefaultConnectedAccount;
        OnboardingLinkResult = null;
    }

    private static Session DefaultCheckoutSession(string sessionId) => new() { Id = sessionId, Object = "checkout.session" };

    private static Session DefaultCreatedCheckoutSession(CheckoutSessionRequest request) => new()
    {
        Id = $"cs_test_{request.IdempotencyKey}",
        Object = "checkout.session",
        Status = "open",
        Url = $"https://checkout.stripe.com/c/pay/cs_test_{request.IdempotencyKey}",
        ExpiresAt = request.ExpiresAt
    };

    private static PaymentIntent DefaultPaymentIntent(string paymentIntentId) => new()
    {
        Id = paymentIntentId,
        Object = "payment_intent",
        LatestChargeId = $"ch_test_{paymentIntentId}"
    };

    private static Account DefaultAccount(string accountId) => new() { Id = accountId, Object = "account" };

    private static Account DefaultConnectedAccount(ConnectedAccountRequest request) => new()
    {
        Id = $"acct_test_user_{request.UserId}",
        Object = "account",
        Country = request.Country
    };
}
