using HotChocolate.Resolvers;
using TournamentAPI.Data.Models;

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
        descriptor.Field(u => u.Id).IsProjected(true);
    }

    [BindMember(nameof(ApplicationUser.Email))]
    public static string? GetEmail([Parent] ApplicationUser user, IResolverContext ctx)
        => user.IsEmailPublic || IsViewingOwnAccount(ctx, user) ? user.Email : null;

    private static bool IsViewingOwnAccount(IResolverContext ctx, ApplicationUser user)
    {
        var viewerId = ctx.GetGlobalStateOrDefault<string>("userId");

        return viewerId != null && int.Parse(viewerId) == user.Id;
    }
}
