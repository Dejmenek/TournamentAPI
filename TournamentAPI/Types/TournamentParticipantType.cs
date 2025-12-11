using TournamentAPI.Models;

namespace TournamentAPI.Types;

public class TournamentParticipantType : ObjectType<TournamentParticipant>
{
    protected override void Configure(IObjectTypeDescriptor<TournamentParticipant> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(tp => tp.TournamentId);
        descriptor.Field(tp => tp.ParticipantId);
        descriptor.Field(tp => tp.Participant)
            .Type<ApplicationUserType>();
        descriptor.Field(tp => tp.Tournament)
            .Type<TournamentType>();
    }
}
