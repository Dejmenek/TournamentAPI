using GreenDonut.Data;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Tournaments;

namespace TournamentAPI.Brackets;

[MutationType]
public static partial class BracketMutations
{
    [UseFirstOrDefault]
    [Authorize]
    public static async Task<IQueryable<Bracket>?> GenerateBracket(
        int tournamentId,
        QueryContext<Bracket> query,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Bracket)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, tournamentId))) return null;
        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament!.OwnerId, userId, tournamentId))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateTournamentIsClosed(tournament))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateBracketDoesNotExist(tournament))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateEnoughParticipants(tournament.Participants.Count, tournamentId))) return null;

        var participantIds = tournament.Participants.Select(p => p.ParticipantId).ToList();
        var bracket = BracketService.CreateBracket(tournamentId, participantIds);

        try
        {
            context.Brackets.Add(bracket);
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            resolverContext.ReportError(BracketErrors.BracketAlreadyExistsForTournament(tournament.Id));
            return null;
        }

        return context.Brackets.AsNoTracking().Where(b => b.Id == bracket.Id).With(query);
    }

    [UseFirstOrDefault]
    [Authorize]
    public static async Task<IQueryable<Bracket>?> UpdateRound(
        int bracketId,
        int roundNumber,
        QueryContext<Bracket> query,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var bracket = await context.Brackets
            .Include(b => b.Tournament)
            .Include(b => b.Matches)
            .FirstOrDefaultAsync(b => b.Id == bracketId, token);

        if (resolverContext.TryReportError(BracketMutationValidations.ValidateBracketExists(bracket, bracketId))) return null;
        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(bracket!.Tournament.OwnerId, userId, bracket.TournamentId))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateTournamentIsClosedForRoundUpdate(bracket.Tournament))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateNextRoundNotGenerated(bracket.Matches, roundNumber, bracketId))) return null;

        var matchesInRound = bracket.Matches.Where(m => m.Round == roundNumber).OrderBy(m => m.Id).ToList();

        if (resolverContext.TryReportError(BracketMutationValidations.ValidateMatchesExistInRound(matchesInRound, roundNumber))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateAllMatchesCompleted(matchesInRound, roundNumber))) return null;

        var winners = matchesInRound.Select(m => m.WinnerId!.Value).ToList();

        if (resolverContext.TryReportError(BracketMutationValidations.ValidateNotFinalRound(winners, bracketId))) return null;

        var newMatches = BracketService.CreateNextRoundMatches(bracket.Id, roundNumber, winners);

        foreach (var match in matchesInRound)
        {
            context.Entry(match).Property(m => m.WinnerId).IsModified = true;
        }

        try
        {
            context.Matches.AddRange(newMatches);
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            resolverContext.ReportError(BracketErrors.RoundDataChangedConcurrently(bracketId));
            return null;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            resolverContext.ReportError(BracketErrors.NextRoundAlreadyGenerated(bracketId));
            return null;
        }

        return context.Brackets.AsNoTracking().Where(b => b.Id == bracket.Id).With(query);
    }
}
