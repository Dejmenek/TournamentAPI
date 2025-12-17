using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Inputs;
using TournamentAPI.Models;

namespace TournamentAPI.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class ParticipantMutations
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ParticipantMutations(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [Authorize]
    public async Task<Tournament> AddParticipant(AddParticipantInput input, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var tournament = await context.Tournaments
        .Include(t => t.Participants)
        .FirstOrDefaultAsync(t => t.Id == input.TournamentId)
        ?? throw new GraphQLException("Tournament doesn't exist");

        if (tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can add participants.");

        if (tournament.Status == TournamentStatus.Closed)
            throw new GraphQLException("Tournament is closed");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == input.UserId)
            ?? throw new GraphQLException("User doesn't exist");

        bool alreadyParticipates = tournament.Participants.Any(tp => tp.ParticipantId == input.UserId);
        if (alreadyParticipates)
            throw new GraphQLException("User already participates in the tournament");

        var participant = new TournamentParticipant
        {
            TournamentId = input.TournamentId,
            ParticipantId = input.UserId
        };

        context.TournamentParticipants.Add(participant);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new GraphQLException("Failed to add participant due to a database error.");
        }

        return tournament;
    }
}
