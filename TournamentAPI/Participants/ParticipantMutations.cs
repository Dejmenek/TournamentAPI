using GreenDonut.Data;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Metrics;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;

namespace TournamentAPI.Participants;

[MutationType]
public static partial class ParticipantMutations
{
    [UseFirstOrDefault]
    [Authorize]
    public static async Task<IQueryable<Tournament>?> AddParticipant(
        AddParticipantInput input,
        QueryContext<Tournament> query,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        ParticipantMetrics participantMetrics,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
        .Include(t => t.Participants)
        .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, input.TournamentId)))
            return null;

        if (tournament is null)
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament.OwnerId, userId, input.TournamentId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentIsNotClosed(tournament, DateTime.UtcNow)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentNotFull(tournament)))
            return null;

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == input.UserId, token);

        if (resolverContext.TryReportError(UserValidations.ValidateUserExists(user, input.UserId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateUserNotAlreadyParticipant(tournament, input.UserId)))
            return null;

        var participant = new TournamentParticipant
        {
            TournamentId = input.TournamentId,
            ParticipantId = input.UserId,
            SlotNumber = tournament.Participants.Count + 1
        };

        context.TournamentParticipants.Add(participant);

        try
        {
            await context.SaveChangesAsync(token);
            return context.Tournaments.AsNoTracking().Where(t => t.Id == input.TournamentId).With(query);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation(TournamentParticipant.SlotNumberUniqueIndexName))
        {
            participantMetrics.SlotContention();
            resolverContext.ReportError(TournamentErrors.TournamentFull(input.TournamentId, tournament.MaxParticipants));
            return null;
        }
        catch (DbUpdateException)
        {
            resolverContext.ReportError(TournamentErrors.UserAlreadyParticipant(input.UserId, input.TournamentId));
            return null;
        }
    }
}
