using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;

namespace TournamentAPI.Users;

[DataLoaderGroup("UserPlayedTournamentIdsBatchingContext")]
internal static class UserPlayedTournamentIdsDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, int[]>> GetPlayedTournamentIdsByParticipantIdAsync(
        IReadOnlyList<int> participantIds,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        participantIds = [.. participantIds.OrderBy(x => x)];
        return await context.TournamentParticipants
            .Where(tp => participantIds.Contains(tp.ParticipantId))
            .GroupBy(tp => tp.ParticipantId)
            .Select(g => new { g.Key, Ids = g.Select(tp => tp.TournamentId).ToArray() })
            .ToDictionaryAsync(g => g.Key, g => g.Ids, cancellationToken);
    }
}
