using TournamentAPI.Models;

namespace TournamentAPI.Types;

public class TournamentType : ObjectType<Tournament>
{
    protected override void Configure(IObjectTypeDescriptor<Tournament> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.StartDate);
        descriptor.Field(t => t.Status);
        descriptor.Field(t => t.OwnerId);
        descriptor.Field(t => t.Bracket)
            .Type<BracketType>();
        descriptor.Field(t => t.Owner)
            .Type<ApplicationUserType>();
        descriptor.Field(t => t.Participants)
            .Type<ListType<TournamentParticipantType>>();
    }
}
