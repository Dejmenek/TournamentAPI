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
        descriptor.Field(t => t.StartDate)
            .IsProjected(true);
        descriptor.Field(t => t.Status)
            .IsProjected(true);
        descriptor.Field("isActive")
            .Type<NonNullType<BooleanType>>()
            .Resolve(ctx => ctx.Parent<Tournament>().IsActive(DateTime.UtcNow));
        descriptor.Field(t => t.OwnerId);
        descriptor.Field(t => t.MaxParticipants);
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
