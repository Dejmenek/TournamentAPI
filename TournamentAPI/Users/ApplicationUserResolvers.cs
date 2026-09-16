using GreenDonut.Data;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;
using TournamentAPI.Tournaments;

namespace TournamentAPI.Users;

[ObjectType<ApplicationUser>]
public static partial class ApplicationUserResolvers
{
    static partial void Configure(IObjectTypeDescriptor<ApplicationUser> descriptor)
    {
        descriptor.Ignore(u => u.UserName);
        descriptor.Ignore(u => u.NormalizedUserName);
        descriptor.Ignore(u => u.NormalizedEmail);
        descriptor.Ignore(u => u.EmailConfirmed);
        descriptor.Ignore(u => u.PasswordHash);
        descriptor.Ignore(u => u.SecurityStamp);
        descriptor.Ignore(u => u.ConcurrencyStamp);
        descriptor.Ignore(u => u.PhoneNumber);
        descriptor.Ignore(u => u.PhoneNumberConfirmed);
        descriptor.Ignore(u => u.TwoFactorEnabled);
        descriptor.Ignore(u => u.LockoutEnd);
        descriptor.Ignore(u => u.LockoutEnabled);
        descriptor.Ignore(u => u.AccessFailedCount);
        descriptor.Ignore(u => u.Email);
        descriptor.Field(u => u.Id).IsProjected(true);
    }

    public static string? GetEmail([Parent] ApplicationUser user, IResolverContext ctx)
        => user.IsEmailPublic || IsViewingOwnAccount(ctx, user) ? user.Email : null;

    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Tournament>> GetPlayedTournaments(
        [Parent(requires: nameof(ApplicationUser.Id))] ApplicationUser user,
        PagingArguments pagingArgs,
        QueryContext<Tournament> query,
        UserTournamentsService userTournamentsService,
        CancellationToken cancellationToken)
        => await userTournamentsService.GetPlayedTournamentsAsync(user.Id, pagingArgs, query, cancellationToken);

    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Tournament>> GetWonTournaments(
        [Parent(requires: nameof(ApplicationUser.Id))] ApplicationUser user,
        PagingArguments pagingArgs,
        QueryContext<Tournament> query,
        UserTournamentsService userTournamentsService,
        CancellationToken cancellationToken)
        => await userTournamentsService.GetWonTournamentsAsync(user.Id, pagingArgs, query, cancellationToken);

    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Match>> GetWonMatches(
        [Parent(requires: nameof(ApplicationUser.Id))] ApplicationUser user,
        PagingArguments pagingArgs,
        QueryContext<Match> query,
        MatchService matchService,
        CancellationToken cancellationToken)
        => await matchService.GetWonMatchesByUserAsync(user.Id, pagingArgs, query, cancellationToken);

    private static bool IsViewingOwnAccount(IResolverContext ctx, ApplicationUser user)
    {
        var viewerId = ctx.GetGlobalStateOrDefault<string>("userId");

        return viewerId != null && int.Parse(viewerId) == user.Id;
    }
}
