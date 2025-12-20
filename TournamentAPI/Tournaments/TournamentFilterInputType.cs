using HotChocolate.Data.Filters;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public class TournamentFilterInputType : FilterInputType<Tournament>
{
    protected override void Configure(IFilterInputTypeDescriptor<Tournament> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.StartDate);
        descriptor.Field(t => t.Status);
        descriptor.Field(t => t.OwnerId);
        descriptor.Field(t => t.Owner);
        descriptor.Field(t => t.Bracket);
        descriptor.Field(t => t.Participants);
    }
}
