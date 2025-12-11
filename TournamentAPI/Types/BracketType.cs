using TournamentAPI.DataLoaders;
using TournamentAPI.Models;

namespace TournamentAPI.Types;

public class BracketType : ObjectType<Bracket>
{
    protected override void Configure(IObjectTypeDescriptor<Bracket> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(b => b.Id);
        descriptor.Field(b => b.TournamentId);
        descriptor.Field(b => b.Matches)
            .ResolveWith<BracketResolvers>(b => b.GetMatchesAsync(default!, default!, default))
            .Type<ListType<MatchType>>();
    }

    private class BracketResolvers
    {
        public async Task<IEnumerable<Match>> GetMatchesAsync(
            [Parent] Bracket bracket,
            MatchesByBracketIdDataLoader dataLoader,
            CancellationToken cancellationToken)
            => (await dataLoader.LoadAsync(bracket.Id, cancellationToken))!;
    }
}
