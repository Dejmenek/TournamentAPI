using TournamentAPI.Models;

namespace TournamentAPI.Types;

public class BracketType : ObjectType<Bracket>
{
    protected override void Configure(IObjectTypeDescriptor<Bracket> descriptor)
    {
        descriptor.Field(b => b.Id);
        descriptor.Field(b => b.TournamentId);
        descriptor.Field(b => b.Matches).Type<ListType<MatchType>>();
    }
}
