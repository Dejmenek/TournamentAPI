using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using TournamentAPI.Brackets;
using TournamentAPI.Data.Models;
using TournamentAPI.Participants;
using TournamentAPI.Users;

namespace TournamentAPI.Tournaments;

[ObjectType<Tournament>]
public static partial class TournamentResolvers
{
    public static bool GetIsActive([Parent] Tournament tournament)
        => tournament.IsActive(DateTime.UtcNow);

    public static async Task<ApplicationUser?> GetOwner(
        [Parent(requires: nameof(Tournament.OwnerId))] Tournament tournament,
        ApplicationUserService applicationUserService,
        CancellationToken cancellationToken)
        => await applicationUserService.GetApplicationUserByIdAsync(tournament.OwnerId, cancellationToken);

    public static async Task<Bracket?> GetBracket(
        [Parent(requires: nameof(Tournament.Id))] Tournament tournament,
        BracketLookupService bracketLookupService,
        CancellationToken cancellationToken)
        => await bracketLookupService.GetBracketByTournamentIdAsync(tournament.Id, cancellationToken);

    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<TournamentParticipant>> GetParticipants(
        [Parent(requires: nameof(Tournament.Id))] Tournament tournament,
        PagingArguments pagingArgs,
        QueryContext<TournamentParticipant> query,
        ParticipantsService participantsService,
        CancellationToken cancellationToken)
    {
        var page = await participantsService.GetParticipantsByTournamentAsync(tournament.Id, pagingArgs, query, cancellationToken);
        return new PageConnection<TournamentParticipant>(page);
    }
}
