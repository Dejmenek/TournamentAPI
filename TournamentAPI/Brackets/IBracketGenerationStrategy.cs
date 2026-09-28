using TournamentAPI.Data.Models;

namespace TournamentAPI.Brackets;

public interface IBracketGenerationStrategy
{
    TournamentFormat Format { get; }

    Bracket CreateBracket(int tournamentId, IList<int> participantIds);
}
