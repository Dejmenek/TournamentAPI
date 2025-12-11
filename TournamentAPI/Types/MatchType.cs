using TournamentAPI.Models;

namespace TournamentAPI.Types;

public class MatchType : ObjectType<Match>
{
    protected override void Configure(IObjectTypeDescriptor<Match> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(m => m.Id);
        descriptor.Field(m => m.Round);
        descriptor.Field(m => m.BracketId);
        descriptor.Field(m => m.Player1Id);
        descriptor.Field(m => m.Player2Id);
        descriptor.Field(m => m.WinnerId);
        descriptor.Field(m => m.Player1).Type<ApplicationUserType>();
        descriptor.Field(m => m.Player2).Type<ApplicationUserType>();
        descriptor.Field(m => m.Winner).Type<ApplicationUserType>();
    }
}
