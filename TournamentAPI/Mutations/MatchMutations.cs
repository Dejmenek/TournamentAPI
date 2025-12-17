using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Models;

namespace TournamentAPI.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class MatchMutations
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public MatchMutations(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [Authorize]
    public async Task<bool> Play(int matchId, int winnerId, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var match = await context.Matches
            .Include(m => m.Bracket)
                .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new GraphQLException("Match doesn't exist");

        var tournament = match.Bracket.Tournament;

        if (tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can record match results.");

        if (tournament.Status != TournamentStatus.Closed)
            throw new GraphQLException("Matches can only be played when the tournament is closed.");

        if (match.WinnerId != null)
            throw new GraphQLException("Match has already been played.");

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
            throw new GraphQLException("Winner must be one of the match participants.");

        match.WinnerId = winnerId;
        await context.SaveChangesAsync();

        return true;
    }
}
