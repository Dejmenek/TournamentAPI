using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
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
            resolverContext.ReportError(BracketErrors.BracketAlreadyExists(tournament.Bracket.Id));
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

        context.Brackets.Add(bracket);
        tournament.Bracket = bracket;

        await context.SaveChangesAsync(token);

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

        var winners = matchesInRound.Select(m => m.WinnerId).ToList();

        if (winners.Count < 2)
        {
            resolverContext.ReportError(BracketErrors.BracketAlreadyHasWinner(bracketId));
            return null;
        }

        for (int i = 0; i < winners.Count; i += 2)
        {
            Match match = new()
            {
                Round = roundNumber + 1,
                Player1Id = (int)winners[i]!,
                Player2Id = i + 1 < winners.Count ? winners[i + 1] : null,
                BracketId = bracket.Id,
            };
            bracket.Matches.Add(match);
        }

        await context.SaveChangesAsync(token);

        return context.Brackets.Where(b => b.Id == bracket.Id);
    }
}
