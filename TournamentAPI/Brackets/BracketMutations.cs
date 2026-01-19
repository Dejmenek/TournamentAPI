using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Tournaments;

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

        if (tournament == null)
        {
            resolverContext.ReportError(TournamentErrors.TournamentNotFound(tournamentId));
            return null;
        }

        if (tournament.OwnerId != userId)
        {
            resolverContext.ReportError(TournamentErrors.TournamentNotOwner(userId, tournamentId));
            return null;
        }

        if (tournament.Status != TournamentStatus.Closed)
        {
            resolverContext.ReportError(BracketErrors.BracketGenerationNotAllowed(tournamentId));
            return null;
        }

        if (tournament.Bracket != null)
        {
            resolverContext.ReportError(BracketErrors.BracketAlreadyExistsForTournament(tournamentId));
            return null;
        }

        if (tournament.Participants.Count < 2)
        {
            resolverContext.ReportError(BracketErrors.NotEnoughParticipants(tournament.Participants.Count));
            return null;
        }

        var bracket = new Bracket
        {
            TournamentId = tournamentId,
            Matches = new List<Match>()
        };

        var participantIds = tournament.Participants.Select(p => p.ParticipantId).ToList();
        var random = new Random();
        participantIds = [.. participantIds.OrderBy(x => random.Next())];

        for (int i = 0; i < participantIds.Count; i += 2)
        {
            Match match = new()
            {
                Round = 1,
                Player1Id = participantIds[i],
                Player2Id = i + 1 < participantIds.Count ? participantIds[i + 1] : null,
                Bracket = bracket,
            };

            bracket.Matches.Add(match);
        }

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

        if (bracket == null)
        {
            resolverContext.ReportError(BracketErrors.BracketNotFound(bracketId));
            return null;
        }

        if (bracket.Tournament.OwnerId != userId)
        {
            resolverContext.ReportError(TournamentErrors.TournamentNotOwner(userId, bracket.TournamentId));
            return null;
        }

        var nextRoundExists = bracket.Matches.Any(m => m.Round == roundNumber + 1);

        if (nextRoundExists)
        {
            resolverContext.ReportError(BracketErrors.NextRoundAlreadyGenerated(bracketId));
            return null;
        }

        var matchesInRound = bracket.Matches.Where(m => m.Round == roundNumber).ToList();

        if (matchesInRound.Count == 0)
        {
            resolverContext.ReportError(BracketErrors.NoMatchesInRound(roundNumber));
            return null;
        }

        if (matchesInRound.Any(m => m.WinnerId == null))
        {
            resolverContext.ReportError(BracketErrors.NotAllMatchesPlayed(roundNumber));
            return null;
        }

        var winners = matchesInRound.Select(m => m.WinnerId!.Value).ToList();

        if (winners.Count < 2)
        {
            resolverContext.ReportError(BracketErrors.BracketAlreadyHasWinner(bracketId));
            return null;
        }

        var newMatches = new List<Match>();

        for (int i = 0; i < winners.Count; i += 2)
        {
            int p1 = winners[i];
            int? p2 = i + 1 < winners.Count ? winners[i + 1] : null;

            if (p2 != null && p2 < p1)
            {
                (p1, p2) = (p2.Value, p1);
            }

            newMatches.Add(new Match
            {
                BracketId = bracket.Id,
                Round = roundNumber + 1,
                Player1Id = p1,
                Player2Id = p2
            });
        }

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
