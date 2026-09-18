using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;

namespace TournamentAPI.Users;

public class UserTournamentsService(
    IUserPlayedTournamentIdsBatchingContext playedTournamentIdsBatchingContext,
    IUserWonTournamentIdsBatchingContext wonTournamentIdsBatchingContext,
    IDbContextFactory<ApplicationDbContext> contextFactory)
{
    public async Task<PageConnection<Tournament>> GetPlayedTournamentsAsync(
        int userId,
        PagingArguments pagingArgs,
        QueryContext<Tournament>? query,
        CancellationToken cancellationToken)
    {
        var ids = await playedTournamentIdsBatchingContext.PlayedTournamentIdsByParticipantId.LoadAsync(userId, cancellationToken);
        return await GetPageAsync(ids, pagingArgs, query, cancellationToken);
    }

    public async Task<PageConnection<Tournament>> GetWonTournamentsAsync(
        int userId,
        PagingArguments pagingArgs,
        QueryContext<Tournament>? query,
        CancellationToken cancellationToken)
    {
        var ids = await wonTournamentIdsBatchingContext.WonTournamentIdsByWinnerId.LoadAsync(userId, cancellationToken);
        return await GetPageAsync(ids, pagingArgs, query, cancellationToken);
    }

    private async Task<PageConnection<Tournament>> GetPageAsync(
        int[]? tournamentIds,
        PagingArguments pagingArgs,
        QueryContext<Tournament>? query,
        CancellationToken cancellationToken)
    {
        tournamentIds ??= [];

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var page = await context.Tournaments
            .Where(t => tournamentIds.Contains(t.Id))
            .With(query, s => s.DefaultTournamentOrder())
            .ToPageAsync(pagingArgs, cancellationToken);

        return new PageConnection<Tournament>(page);
    }
}
