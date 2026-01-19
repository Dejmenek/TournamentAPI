using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.Matches;

[ExtendObjectType(typeof(Mutation))]
public class MatchMutations
{
    [Authorize]
    public async Task<bool?> Play(
        int matchId,
        int winnerId,
        ClaimsPrincipal userClaims,
        IResolverContext resolverContext,
        ApplicationDbContext context,
        CancellationToken token)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        var match = await context.Matches
            .Include(m => m.Bracket)
                .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId, token);

        if (match == null)
        {
            resolverContext.ReportError(MatchErrors.MatchNotFound(matchId));
            return null;
        }

        var tournament = match.Bracket.Tournament;

        if (tournament.OwnerId != userId)
        {
            resolverContext.ReportError(TournamentErrors.TournamentNotOwner(userId, tournament.Id));
            return null;
        }

        if (tournament.Status != TournamentStatus.Closed)
        {
            resolverContext.ReportError(MatchErrors.TournamentNotClosed(tournament.Id));
            return null;
        }

        if (match.WinnerId != null)
        {
            resolverContext.ReportError(MatchErrors.MatchAlreadyPlayed(matchId));
            return null;
        }

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
        {
            resolverContext.ReportError(MatchErrors.InvalidMatchWinner(matchId, winnerId));
            return null;
        }

        match.WinnerId = winnerId;

        try
        {
            await context.SaveChangesAsync(token);

            return true;
        }
        catch (DbUpdateException)
        {
            resolverContext.ReportError(MatchErrors.MatchAlreadyPlayed(matchId));
            return null;
        }
    }
}
