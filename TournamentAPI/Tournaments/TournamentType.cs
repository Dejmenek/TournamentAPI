using TournamentAPI.Brackets;
using TournamentAPI.Data.Models;
using TournamentAPI.Participants;
using TournamentAPI.Users;

namespace TournamentAPI.Tournaments;

public class TournamentType : ObjectType<Tournament>
{
    protected override void Configure(IObjectTypeDescriptor<Tournament> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.StartDate);
        descriptor.Field(t => t.Status);
        descriptor.Field(t => t.OwnerId);
        descriptor.Field(t => t.Bracket)
            .Type<BracketType>();
        descriptor.Field(t => t.Owner)
            .Type<ApplicationUserType>()
            .UseFiltering<UserFilterInputType>()
            .UseSorting<UserSortInputType>();
        descriptor.Field(t => t.Participants)
            .Type<ListType<TournamentParticipantType>>();
    }
}
