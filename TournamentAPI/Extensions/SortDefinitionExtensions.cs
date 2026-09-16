using GreenDonut.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Extensions;

public static class SortDefinitionExtensions
{
    public static SortDefinition<Tournament> DefaultTournamentOrder(this SortDefinition<Tournament> sort)
        => sort.IfEmpty(o => o.AddAscending(t => t.Id)).AddAscending(t => t.Id);

    public static SortDefinition<Match> DefaultMatchOrder(this SortDefinition<Match> sort)
        => sort.IfEmpty(o => o.AddAscending(m => m.Id)).AddAscending(m => m.Id);
}
