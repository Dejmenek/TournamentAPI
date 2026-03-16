using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;

namespace TournamentAPI.Brackets;

[ExtendObjectType(typeof(Mutation))]
public class BracketMutations
{
    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Bracket>?> GenerateBracket(
        int tournamentId,
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
            .Include(t => t.Bracket)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        if (resolverContext.TryReportError(BracketMutationValidations.ValidateTournamentExists(tournament, tournamentId))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateIsOwner(tournament!.OwnerId, userId, tournamentId))) return null;
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

        return context.Brackets.Where(b => b.Id == bracket.Id);
    }

    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Bracket>?> UpdateRound(
        int bracketId,
        int roundNumber,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        CancellationToken token)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        var bracket = await context.Brackets
            .Include(b => b.Tournament)
            .Include(b => b.Matches)
            .FirstOrDefaultAsync(b => b.Id == bracketId, token);

        if (resolverContext.TryReportError(BracketMutationValidations.ValidateBracketExists(bracket, bracketId))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateIsOwner(bracket!.Tournament.OwnerId, userId, bracket.TournamentId))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateNextRoundNotGenerated(bracket.Matches, roundNumber, bracketId))) return null;

        var matchesInRound = bracket.Matches.Where(m => m.Round == roundNumber).ToList();

        if (resolverContext.TryReportError(BracketMutationValidations.ValidateMatchesExistInRound(matchesInRound, roundNumber))) return null;
        if (resolverContext.TryReportError(BracketMutationValidations.ValidateAllMatchesCompleted(matchesInRound, roundNumber))) return null;

        var winners = matchesInRound.Select(m => m.WinnerId!.Value).ToList();

        if (resolverContext.TryReportError(BracketMutationValidations.ValidateNotFinalRound(winners, bracketId))) return null;

        var newMatches = BracketService.CreateNextRoundMatches(bracket.Id, roundNumber, winners);

        try
        {
            context.Matches.AddRange(newMatches);
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            resolverContext.ReportError(BracketErrors.NextRoundAlreadyGenerated(bracketId));
            return null;
        }

        return context.Brackets.Where(b => b.Id == bracket.Id);
    }
}
