using HotChocolate.Data.Sorting;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

public class UserSortInputType : SortInputType<ApplicationUser>
{
    protected override void Configure(ISortInputTypeDescriptor<ApplicationUser> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(u => u.Id);
        descriptor.Field(u => u.FirstName);
        descriptor.Field(u => u.LastName);
        descriptor.Field(u => u.Email);
    }
}
