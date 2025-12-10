using TournamentAPI.Models;

namespace TournamentAPI.Types;

public class ApplicationUserType : ObjectType<ApplicationUser>
{
    protected override void Configure(IObjectTypeDescriptor<ApplicationUser> descriptor)
    {
        descriptor.Field(u => u.Id);
        descriptor.Field(u => u.FirstName);
        descriptor.Field(u => u.LastName);
        descriptor.Field(u => u.Email);
        descriptor.Field(u => u.MatchesAsPlayer1)
            .Type<ListType<MatchType>>();
        descriptor.Field(u => u.MatchesAsPlayer2)
            .Type<ListType<MatchType>>();
        descriptor.Field(u => u.MatchesWon)
            .Type<ListType<MatchType>>();
    }
}
