using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.Standings;

[ObjectType<Bracket>]
public static partial class StandingsResolvers
{
    public static async Task<IReadOnlyList<StandingEntry>> GetStandings(
        [Parent] Bracket bracket,
        TournamentLookupService tournamentLookupService,
        StandingsLookupService standingsLookupService,
        StandingsService standingsService,
        CancellationToken cancellationToken)
    {
        var tournament = await tournamentLookupService.GetTournamentByIdAsync(bracket.TournamentId, cancellationToken);

        if (tournament?.Format != TournamentFormat.RoundRobin)
            return [];

        var matches = await standingsLookupService.GetMatchesByBracketIdAsync(bracket.Id, cancellationToken);

        return standingsService.ComputeStandings(matches);
    }
}
