using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

public class ApplicationUserType : ObjectType<ApplicationUser>
{
    protected override void Configure(IObjectTypeDescriptor<ApplicationUser> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(u => u.Id);
        descriptor.Field(u => u.FirstName);
        descriptor.Field(u => u.LastName);
        descriptor.Field(u => u.Email);
    }
}
