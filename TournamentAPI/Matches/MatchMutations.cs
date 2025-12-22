using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.Matches;

[ExtendObjectType(typeof(Mutation))]
public class MatchMutations
{
    [Error<MatchNotFoundException>]
    [Error<TournamentNotOwnerException>]
    [Error<TournamentNotClosedException>]
    [Error<MatchAlreadyPlayedException>]
    [Error<InvalidMatchWinnerException>]
    [Authorize]
    public async Task<bool> Play(
        int matchId,
        int winnerId,
        ClaimsPrincipal userClaims,
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
            .FirstOrDefaultAsync(m => m.Id == matchId, token)
            ?? throw new MatchNotFoundException();

        var tournament = match.Bracket.Tournament;

        if (tournament.OwnerId != userId)
            throw new TournamentNotOwnerException();

        if (tournament.Status != TournamentStatus.Closed)
            throw new TournamentNotClosedException();

        if (match.WinnerId != null)
            throw new MatchAlreadyPlayedException();

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
            throw new InvalidMatchWinnerException();

        match.WinnerId = winnerId;
        await context.SaveChangesAsync(token);

        return true;
    }
}
