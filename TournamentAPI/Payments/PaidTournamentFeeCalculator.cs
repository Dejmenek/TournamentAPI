namespace TournamentAPI.Payments;

/// <summary>
/// Pure money arithmetic for paid tournaments. All amounts are minor units (cents).
/// </summary>
public static class PaidTournamentFeeCalculator
{
    public static long CalculatePlatformFee(long amount, decimal percent)
        => (long)Math.Ceiling((amount * percent) / 100m);

    public static long CalculateEscrowAmount(IEnumerable<long> prizeAmounts, decimal percent)
    {
        var prizeTotal = prizeAmounts.Sum();
        return prizeTotal + CalculatePlatformFee(prizeTotal, percent);
    }

    /// <summary>
    /// Splits the combined prize money of the placements a tie covers equally between the tied users.
    /// Leftover cents go one each to the tied users in ascending user ID order.
    /// </summary>
    /// <param name="prizesByPlacement">Prize amount per placement; placements without a prize pay nothing.</param>
    /// <param name="firstPlacement">The placement the tied users share.</param>
    /// <param name="userIds">The tied users.</param>
    public static IReadOnlyDictionary<int, long> SplitTiedPrizes(
        IReadOnlyDictionary<int, long> prizesByPlacement,
        int firstPlacement,
        IReadOnlyCollection<int> userIds)
    {
        var orderedUserIds = userIds.Order().ToList();
        var count = orderedUserIds.Count;

        long pool = 0;
        for (var placement = firstPlacement; placement < firstPlacement + count; placement++)
        {
            if (prizesByPlacement.TryGetValue(placement, out var amount))
                pool += amount;
        }

        var share = pool / count;
        var leftover = pool % count;

        var result = new Dictionary<int, long>(count);
        for (var i = 0; i < count; i++)
            result[orderedUserIds[i]] = share + (i < leftover ? 1 : 0);

        return result;
    }
}
