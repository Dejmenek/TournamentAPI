using GreenDonut.Data;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Participants;

[DataLoaderGroup("TournamentParticipantBatchingContext")]
internal static class ParticipantsDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Page<TournamentParticipant>>> GetParticipantsByTournamentAsync(
        IReadOnlyList<int> tournamentIds,
        PagingArguments pagingArgs,
        QueryContext<TournamentParticipant> query,
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        tournamentIds = [.. tournamentIds.OrderBy(x => x)];
        return await context.TournamentParticipants
            .Where(tp => tournamentIds.Contains(tp.TournamentId))
            .With(query, DefaultOrder)
            .ToBatchPageAsync(
                tp => tp.TournamentId,
                pagingArgs,
                cancellationToken
            );
    }

    private static SortDefinition<TournamentParticipant> DefaultOrder(SortDefinition<TournamentParticipant> sort)
    {
        return sort.IfEmpty(o => o.AddAscending(tp => tp.SlotNumber)).AddAscending(tp => tp.ParticipantId);
    }
}
