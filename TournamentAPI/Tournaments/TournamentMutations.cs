using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

[ExtendObjectType(typeof(Mutation))]
public class TournamentMutations
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public TournamentMutations(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [Authorize]
    public async Task<bool> JoinTournament(int tournamentId, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var tournament = await context.Tournaments
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == tournamentId)
            ?? throw new GraphQLException("Tournament doesn't exist");

        if (tournament.Status == TournamentStatus.Closed)
            throw new GraphQLException("Tournament is closed");

        bool alreadyParticipates = tournament.Participants.Any(tp => tp.ParticipantId == userId);
        if (alreadyParticipates)
            throw new GraphQLException("User already participates in the tournament");

        var participant = new TournamentParticipant
        {
            TournamentId = tournamentId,
            ParticipantId = userId
        };

        context.TournamentParticipants.Add(participant);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new GraphQLException("Failed to join tournament due to a database error.");
        }

        return true;
    }

    [Authorize]
    public async Task<Tournament> CreateTournament(CreateTournamentInput input, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new GraphQLException("Tournament name cannot be empty.");
        }

        var tournament = new Tournament
        {
            Name = input.Name,
            StartDate = input.StartDate,
            Status = input.Status,
            OwnerId = userId
        };

        using var context = _contextFactory.CreateDbContext();

        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync();

        return tournament;
    }

    [Authorize]
    public async Task<Tournament> UpdateTournament(UpdateTournamentInput input, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var tournament = await context.Tournaments.FirstOrDefaultAsync(t => t.Id == input.TournamentId)
            ?? throw new GraphQLException("Tournament doesn't exist");

        if (tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can update the tournament.");

        if (input.Name != null)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new GraphQLException("Tournament name cannot be empty.");
            tournament.Name = input.Name;
        }

        if (input.StartDate != null)
            tournament.StartDate = input.StartDate.Value;

        if (input.Status != null)
            tournament.Status = input.Status.Value;

        await context.SaveChangesAsync();

        return tournament;
    }

    [Authorize]
    public async Task<bool> DeleteTournament(int tournamentId, ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var tournament = await context.Tournaments
        .Include(t => t.Bracket)
            .ThenInclude(b => b.Matches)
        .Include(t => t.Participants)
        .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament is null) return false;

        if (tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can delete the tournament.");

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

        await context.SaveChangesAsync();

        return true;
    }
}
