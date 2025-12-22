using TournamentAPI.Data.Models;
using MatchType = TournamentAPI.Matches.MatchType;

namespace TournamentAPI.Brackets;

public class BracketType : ObjectType<Bracket>
{
    protected override void Configure(IObjectTypeDescriptor<Bracket> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(b => b.Id);
        descriptor.Field(b => b.TournamentId);
        descriptor.Field(b => b.Matches)
            .Type<ListType<MatchType>>();
    }
}
