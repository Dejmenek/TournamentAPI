using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Brackets;

public interface IBracketCompletionStrategy
{
    TournamentFormat Format { get; }

    Task SyncCompletionAsync(
        ApplicationDbContext context,
        Tournament tournament,
        int bracketId,
        int frontierRound,
        CancellationToken token);
}
