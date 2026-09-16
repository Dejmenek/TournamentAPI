using TournamentAPI.Data.Models;
using TournamentAPI.Users;

namespace TournamentAPI.Matches;

[ObjectType<Match>]
public static partial class MatchResolvers
{
    public static async Task<ApplicationUser?> GetPlayer1(
        [Parent(requires: nameof(Match.Player1Id))] Match match,
        ApplicationUserService applicationUserService,
        CancellationToken cancellationToken)
        => await applicationUserService.GetApplicationUserByIdAsync(match.Player1Id, cancellationToken);

    public static async Task<ApplicationUser?> GetPlayer2(
        [Parent(requires: nameof(Match.Player2Id))] Match match,
        ApplicationUserService applicationUserService,
        CancellationToken cancellationToken)
        => match.Player2Id is null ? null : await applicationUserService.GetApplicationUserByIdAsync(match.Player2Id.Value, cancellationToken);

    public static async Task<ApplicationUser?> GetWinner(
        [Parent(requires: nameof(Match.WinnerId))] Match match,
        ApplicationUserService applicationUserService,
        CancellationToken cancellationToken)
        => match.WinnerId is null ? null : await applicationUserService.GetApplicationUserByIdAsync(match.WinnerId.Value, cancellationToken);

    public static string GetVersion(
        [Parent(requires: nameof(Match.RowVersion))] Match match)
        => MatchVersionCodec.Encode(match.RowVersion);
}
