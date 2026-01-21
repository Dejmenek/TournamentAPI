using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;

namespace TournamentAPI.Participants;

[ExtendObjectType(typeof(Mutation))]
public class ParticipantMutations
{
    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Tournament>?> AddParticipant(
        AddParticipantInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        CancellationToken token)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        var tournament = await context.Tournaments
        .Include(t => t.Participants)
        .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token);

        if (tournament == null)
        {
            resolverContext.ReportError(TournamentErrors.TournamentNotFound(input.TournamentId));
            return null;
        }

        if (tournament.OwnerId != userId)
        {
            resolverContext.ReportError(TournamentErrors.TournamentNotOwner(userId, input.TournamentId));
            return null;
        }

        if (tournament.Status == TournamentStatus.Closed)
        {
            resolverContext.ReportError(TournamentErrors.TournamentClosed(input.TournamentId));
            return null;
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == input.UserId, token);

        if (user == null)
        {
            resolverContext.ReportError(UserErrors.UserNotFound(input.UserId));
            return null;
        }

        bool alreadyParticipates = tournament.Participants.Any(tp => tp.ParticipantId == input.UserId);
        if (alreadyParticipates)
        {
            resolverContext.ReportError(TournamentErrors.UserAlreadyParticipant(input.UserId, input.TournamentId));
            return null;
        }

        var participant = new TournamentParticipant
        {
            TournamentId = input.TournamentId,
            ParticipantId = input.UserId
        };

        context.TournamentParticipants.Add(participant);

        try
        {
            await context.SaveChangesAsync(token);
            return context.Tournaments.AsNoTracking().Where(t => t.Id == input.TournamentId);
        }
        catch (DbUpdateException)
        {
            resolverContext.ReportError(TournamentErrors.UserAlreadyParticipant(input.UserId, input.TournamentId));
            return null;
        }
    }
}
