using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;

namespace TournamentAPI.Participants;

[ObjectType<TournamentParticipant>]
public static partial class TournamentParticipantResolvers
{
    public static async Task<ApplicationUser?> GetParticipant(
        [Parent(requires: nameof(TournamentParticipant.ParticipantId))] TournamentParticipant tournamentParticipant,
        ApplicationUserService applicationUserService,
        CancellationToken cancellationToken)
        => await applicationUserService.GetApplicationUserByIdAsync(tournamentParticipant.ParticipantId, cancellationToken);

    public static async Task<Tournament?> GetTournament(
        [Parent(requires: nameof(TournamentParticipant.TournamentId))] TournamentParticipant tournamentParticipant,
        TournamentLookupService tournamentLookupService,
        CancellationToken cancellationToken)
        => await tournamentLookupService.GetTournamentByIdAsync(tournamentParticipant.TournamentId, cancellationToken);
}
