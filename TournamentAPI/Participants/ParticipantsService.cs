using GreenDonut.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Participants;

public class ParticipantsService(ITournamentParticipantBatchingContext batchingContext)
{
    public async Task<Page<TournamentParticipant>> GetParticipantsByTournamentAsync(
        int tournamentId,
        PagingArguments pagingArgs,
        QueryContext<TournamentParticipant>? query = null,
        CancellationToken cancellationToken = default
    )
    {
        return await batchingContext.ParticipantsByTournament
            .With(pagingArgs, query)
            .LoadAsync(tournamentId, cancellationToken)
            ?? Page<TournamentParticipant>.Empty;
    }
}
