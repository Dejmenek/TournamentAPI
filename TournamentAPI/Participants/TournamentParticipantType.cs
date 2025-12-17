using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;

namespace TournamentAPI.Participants;

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
