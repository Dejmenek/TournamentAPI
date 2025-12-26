using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.Brackets;

[ExtendObjectType(typeof(Mutation))]
public class BracketMutations
{
    [Error<BracketAlreadyExistsException>]
    [Error<NotEnoughParticipantsException>]
    [Error<TournamentNotFoundException>]
    [Error<TournamentNotOwnerException>]
    [Error<BracketGenerationNotAllowedException>]
    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Bracket>> GenerateBracket(
        int tournamentId,
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
            .Include(t => t.Bracket)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token)
            ?? throw new TournamentNotFoundException();

        if (tournament.OwnerId != userId)
            throw new TournamentNotOwnerException();

        if (tournament.Status != TournamentStatus.Closed)
            throw new BracketGenerationNotAllowedException();

        if (tournament.Bracket != null)
            throw new BracketAlreadyExistsException();

        if (tournament.Participants.Count < 2)
            throw new NotEnoughParticipantsException();

        var bracket = new Bracket
        {
            TournamentId = tournamentId,
            Matches = new List<Match>()
        };

        var participants = tournament.Participants.ToList();
        var random = new Random();
        participants = [.. participants.OrderBy(x => random.Next())];

        for (int i = 0; i < participants.Count; i += 2)
        {
            Match match = new()
            {
                Round = 1,
                Player1Id = participants[i].ParticipantId,
                Player2Id = i + 1 < participants.Count ? participants[i + 1].ParticipantId : null,
                Bracket = bracket,
            };

            bracket.Matches.Add(match);
        }

        context.Brackets.Add(bracket);
        tournament.Bracket = bracket;

        await context.SaveChangesAsync(token);

        return context.Brackets.Where(b => b.Id == bracket.Id);
    }

    [Error<BracketNotFoundException>]
    [Error<TournamentNotOwnerException>]
    [Error<NoMatchesInRoundException>]
    [Error<NotAllMatchesPlayedException>]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Bracket>> UpdateRound(
        int bracketId,
        int roundNumber,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        CancellationToken token)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        var bracket = await context.Brackets
            .Include(b => b.Tournament)
            .Include(b => b.Matches)
            .FirstOrDefaultAsync(b => b.Id == bracketId, token)
            ?? throw new BracketNotFoundException();

        if (bracket.Tournament.OwnerId != userId)
            throw new TournamentNotOwnerException();

        var matchesInRound = bracket.Matches.Where(m => m.Round == roundNumber).ToList();

        if (matchesInRound.Count == 0)
            throw new NoMatchesInRoundException();

        if (matchesInRound.Any(m => m.WinnerId == null))
            throw new NotAllMatchesPlayedException();

        var winners = matchesInRound.Select(m => m.Winner!).ToList();
        for (int i = 0; i < winners.Count; i += 2)
        {
            Match match = new()
            {
                Round = roundNumber + 1,
                Player1Id = winners[i].Id,
                Player2Id = i + 1 < winners.Count ? winners[i + 1].Id : null,
                BracketId = bracket.Id,
            };
            bracket.Matches.Add(match);
        }

        await context.SaveChangesAsync(token);

        return context.Brackets.Where(b => b.Id == bracket.Id);
    }
}
