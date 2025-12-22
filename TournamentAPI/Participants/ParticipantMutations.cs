using HotChocolate.Authorization;
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
    [Error<TournamentNotFoundException>]
    [Error<TournamentNotOwnerException>]
    [Error<TournamentClosedException>]
    [Error<UserNotFoundException>]
    [Error<UserAlreadyParticipantException>]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Tournament>> AddParticipant(
        AddParticipantInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        CancellationToken token)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        var tournament = await context.Tournaments
        .Include(t => t.Participants)
        .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token)
        ?? throw new TournamentNotFoundException();

        if (tournament.OwnerId != userId)
            throw new TournamentNotOwnerException();

        if (tournament.Status == TournamentStatus.Closed)
            throw new TournamentClosedException();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == input.UserId, token)
            ?? throw new UserNotFoundException();

        bool alreadyParticipates = tournament.Participants.Any(tp => tp.ParticipantId == input.UserId);
        if (alreadyParticipates)
            throw new UserAlreadyParticipantException();

        var participant = new TournamentParticipant
        {
            TournamentId = input.TournamentId,
            ParticipantId = input.UserId
        };

        context.TournamentParticipants.Add(participant);

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            throw new GraphQLException("Failed to add participant due to a database error.");
        }

        return context.Tournaments.AsNoTracking().Where(t => t.Id == input.TournamentId);
    }
}
