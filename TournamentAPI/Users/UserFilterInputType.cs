using HotChocolate.Data.Filters;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

public class UserFilterInputType : FilterInputType<ApplicationUser>
{
    protected override void Configure(IFilterInputTypeDescriptor<ApplicationUser> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(u => u.Id);
        descriptor.Field(u => u.FirstName);
        descriptor.Field(u => u.LastName);
        descriptor.Field(u => u.Email);
    }
}
