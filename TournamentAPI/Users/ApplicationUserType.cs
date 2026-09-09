using HotChocolate.Resolvers;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

public class ApplicationUserType : ObjectType<ApplicationUser>
{
    protected override void Configure(IObjectTypeDescriptor<ApplicationUser> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(u => u.Id).IsProjected(true);
        descriptor.Field(u => u.FirstName);
        descriptor.Field(u => u.LastName);
        descriptor.Field(u => u.IsEmailPublic).IsProjected(true);
        descriptor.Field(u => u.Email)
            .Resolve(ctx =>
            {
                var user = ctx.Parent<ApplicationUser>();

                return user.IsEmailPublic || IsViewingOwnAccount(ctx, user)
                    ? user.Email
                    : null;
            });
    }

    private static bool IsViewingOwnAccount(IResolverContext ctx, ApplicationUser user)
    {
        var viewerId = ctx.GetGlobalStateOrDefault<string>("userId");

        return viewerId != null && int.Parse(viewerId) == user.Id;
    }
}
