using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Models;

namespace TournamentAPI.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class BracketMutations
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public BracketMutations(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [Authorize]
    public async Task<Bracket> GenerateBracket(int tournamentId, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var tournament = await context.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Bracket)
            .FirstOrDefaultAsync(t => t.Id == tournamentId)
            ?? throw new GraphQLException("Tournament doesn't exist");

        if (tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can generate the bracket.");

        if (tournament.Status != TournamentStatus.Closed)
            throw new GraphQLException("Bracket can only be generated when the tournament is closed.");

        if (tournament.Bracket != null)
            throw new GraphQLException("Bracket already exists for this tournament.");

        if (tournament.Participants.Count < 2)
            throw new GraphQLException("Not enough participants to create a bracket");

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

        await context.SaveChangesAsync();

        return bracket;
    }

    [Authorize]
    public async Task<Bracket> UpdateRound(int bracketId, int roundNumber, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var bracket = await context.Brackets
            .Include(b => b.Tournament)
            .Include(b => b.Matches)
            .FirstOrDefaultAsync(b => b.Id == bracketId)
            ?? throw new GraphQLException("Bracket doesn't exist");

        if (bracket.Tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can update the bracket.");

        var matchesInRound = bracket.Matches.Where(m => m.Round == roundNumber).ToList();

        if (matchesInRound.Count == 0)
            throw new GraphQLException("No matches found in the specified round.");

        if (matchesInRound.Any(m => m.WinnerId == null))
            throw new GraphQLException("Not all matches in the current round have been played.");

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

        await context.SaveChangesAsync();

        return bracket;
    }
}
