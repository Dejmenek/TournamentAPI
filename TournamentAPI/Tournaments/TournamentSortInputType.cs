using HotChocolate.Data.Sorting;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public class TournamentSortInputType : SortInputType<Tournament>
{
    protected override void Configure(ISortInputTypeDescriptor<Tournament> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.StartDate);
        descriptor.Field(t => t.Status);
        descriptor.Field(t => t.OwnerId);
    }
}
