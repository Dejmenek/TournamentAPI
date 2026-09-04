using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Metrics;

namespace TournamentAPI.Tournaments;

[ExtendObjectType(typeof(Mutation))]
public class TournamentMutations
{
    [Authorize]
    public async Task<bool?> JoinTournament(
        int tournamentId,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, tournamentId)))
            return null;

        if (tournament is null)
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentIsNotClosed(tournament)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentNotFull(tournament)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateUserNotAlreadyParticipant(tournament, userId)))
            return null;

        var participant = new TournamentParticipant
        {
            TournamentId = tournamentId,
            ParticipantId = userId,
            SlotNumber = tournament.Participants.Count + 1
        };

        context.TournamentParticipants.Add(participant);
        try
        {
            await context.SaveChangesAsync(token);
            return true;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation(TournamentParticipant.SlotNumberUniqueIndexName))
        {
            resolverContext.ReportError(
                TournamentErrors.TournamentFull(tournamentId, tournament.MaxParticipants));
            return null;
        }
        catch (DbUpdateException)
        {
            resolverContext.ReportError(
                TournamentErrors.UserAlreadyParticipant(userId, tournamentId));
            return null;
        }
    }

    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Tournament>?> CreateTournament(
        CreateTournamentInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        TournamentMetrics tournamentMetrics,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentNameNotEmpty(input.Name)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateMaxParticipantsAtLeastTwo(input.MaxParticipants)))
            return null;

        var tournament = new Tournament
        {
            Name = input.Name,
            StartDate = input.StartDate,
            Status = input.Status,
            OwnerId = userId,
            MaxParticipants = input.MaxParticipants
        };

        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync(token);

        tournamentMetrics.IncrementTournamentsCreated();
        if (tournament.Status == TournamentStatus.Open)
            tournamentMetrics.TournamentOpened();

        return context.Tournaments.Where(t => t.Id == tournament.Id);
    }

    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Tournament>?> UpdateTournament(
        UpdateTournamentInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        TournamentMetrics tournamentMetrics,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, input.TournamentId)))
            return null;

        if (tournament is null)
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament.OwnerId, userId, input.TournamentId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentNameNotEmpty(input.Name)))
            return null;
        tournament.Name = input.Name;

        if (input.StartDate != null)
            tournament.StartDate = input.StartDate.Value;

        if (input.MaxParticipants != null)
        {
            if (resolverContext.TryReportError(TournamentValidations.ValidateMaxParticipantsAtLeastTwo(input.MaxParticipants.Value)))
                return null;

            if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentIsNotClosed(tournament)))
                return null;

            var currentParticipantCount = await context.TournamentParticipants
                .CountAsync(tp => tp.TournamentId == tournament.Id, token);

            if (resolverContext.TryReportError(TournamentValidations.ValidateMaxParticipantsNotBelowParticipantCount(tournament.Id, currentParticipantCount, input.MaxParticipants.Value)))
                return null;

            tournament.MaxParticipants = input.MaxParticipants.Value;
        }

        if (input.Status != null)
        {
            var previousStatus = tournament.Status;
            tournament.Status = input.Status.Value;

            if (previousStatus != TournamentStatus.Open && tournament.Status == TournamentStatus.Open)
                tournamentMetrics.TournamentOpened();
            else if (previousStatus == TournamentStatus.Open && tournament.Status != TournamentStatus.Open)
                tournamentMetrics.TournamentClosed();
        }

        await context.SaveChangesAsync(token);

        return context.Tournaments.Where(t => t.Id == tournament.Id);
    }

    [Authorize]
    public async Task<bool?> DeleteTournament(
        int tournamentId,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        TournamentMetrics tournamentMetrics,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .Include(t => t.Bracket)
                .ThenInclude(b => b.Matches)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, tournamentId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament!.OwnerId, userId, tournamentId)))
            return null;

        var wasOpen = tournament!.Status == TournamentStatus.Open;

        tournament.IsDeleted = true;

        if (tournament.Bracket != null)
        {
            tournament.Bracket.IsDeleted = true;
            foreach (var match in tournament.Bracket.Matches)
            {
                match.IsDeleted = true;
            }
        }

        foreach (var participant in tournament.Participants)
        {
            participant.IsDeleted = true;
        }

        await context.SaveChangesAsync(token);

        if (wasOpen)
            tournamentMetrics.TournamentClosed();

        return true;
    }
}
