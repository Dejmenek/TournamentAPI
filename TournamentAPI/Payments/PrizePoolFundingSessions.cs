using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Payments;

internal sealed record StoredFundingSession(string? SessionId, int Attempt);

internal static class PrizePoolFundingSessions
{
    public static async Task<StoredFundingSession?> ReadAsync(
        ApplicationDbContext context,
        int tournamentId,
        CancellationToken token)
        => await context.Tournaments
            .Where(t => t.Id == tournamentId)
            .Select(t => new StoredFundingSession(t.FundingSessionId, t.FundingSessionAttempt))
            .FirstOrDefaultAsync(token);

    /// <summary>
    /// Fixes the expiry every caller on the current attempt sends to Stripe, so callers sharing an
    /// idempotency key also share its parameters. Keeps an expiry that still has room before Stripe's
    /// minimum and replaces one that has run short. Returns null when the tournament no longer exists.
    /// </summary>
    public static async Task<DateTime?> ReserveExpiryAsync(
        ApplicationDbContext context,
        int tournamentId,
        int checkoutExpiryMinutes,
        DateTime now,
        CancellationToken token)
    {
        var usableFrom = now.AddMinutes(PrizePoolFunding.StripeMinimumExpiryMinutes).AddSeconds(30);
        var proposed = now.AddMinutes(checkoutExpiryMinutes + PrizePoolFunding.ExpiryMarginMinutes);

        await context.Tournaments
            .Where(t => t.Id == tournamentId
                && t.FundingSessionId == null
                && (t.FundingSessionExpiresAt == null || t.FundingSessionExpiresAt < usableFrom))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.FundingSessionExpiresAt, proposed), token);

        return await context.Tournaments
            .Where(t => t.Id == tournamentId)
            .Select(t => t.FundingSessionExpiresAt)
            .FirstOrDefaultAsync(token);
    }

    public static Task<int> FillAsync(
        ApplicationDbContext context,
        Tournament tournament,
        long prizeTotal,
        Session session,
        DateTime now,
        CancellationToken token)
        => context.Tournaments
            .Where(t => t.Id == tournament.Id
                && t.FundingSessionId == null
                && t.Status == TournamentStatus.Closed
                && t.StartDate > now
                && t.PrizePoolStatus == PrizePoolStatus.Unfunded
                && t.EntryFeeAmount == tournament.EntryFeeAmount
                && t.Prizes.Sum(p => p.Amount) == prizeTotal)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.FundingSessionId, session.Id)
                .SetProperty(t => t.FundingSessionExpiresAt, session.ExpiresAt),
                token);

    /// <summary>
    /// Forgets the stored session and moves the attempt counter, so the next Stripe call uses a fresh idempotency key.
    /// </summary>
    public static Task<int> ResetAsync(
        ApplicationDbContext context,
        string sessionId,
        CancellationToken token)
        => context.Tournaments
            .Where(t => t.FundingSessionId == sessionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.FundingSessionId, (string?)null)
                .SetProperty(t => t.FundingSessionExpiresAt, (DateTime?)null)
                .SetProperty(t => t.FundingSessionAttempt, t => t.FundingSessionAttempt + 1),
                token);

    public static Task<int> AdvanceAttemptAsync(
        ApplicationDbContext context,
        int tournamentId,
        int attempt,
        CancellationToken token)
        => context.Tournaments
            .Where(t => t.Id == tournamentId && t.FundingSessionId == null && t.FundingSessionAttempt == attempt)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.FundingSessionExpiresAt, (DateTime?)null)
                .SetProperty(t => t.FundingSessionAttempt, attempt + 1),
                token);
}
