using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Extensions;
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
        var userId = userClaims.GetUserId();

        var match = await context.Matches
            .Include(m => m.Bracket)
                .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId, token);

        if (resolverContext.TryReportError(MatchValidations.ValidateMatchExists(match, matchId)))
            return null;

        var tournament = match!.Bracket.Tournament;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament.OwnerId, userId, tournament.Id)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateTournamentIsClosed(tournament)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateMatchNotPlayed(match)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateWinnerIsParticipant(match, winnerId)))
            return null;

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
