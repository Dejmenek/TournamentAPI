using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

[ExtendObjectType(typeof(Mutation))]
public class TournamentMutations
{
    [Error<TournamentNotFoundException>]
    [Error<TournamentClosedException>]
    [Error<TournamentJoinFailedException>]
    [Authorize]
    public async Task<bool> JoinTournament(
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
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token)
            ?? throw new TournamentNotFoundException();

        if (tournament.Status == TournamentStatus.Closed)
            throw new TournamentClosedException();

        bool alreadyParticipates = tournament.Participants.Any(tp => tp.ParticipantId == userId);
        if (alreadyParticipates)
            throw new TournamentJoinFailedException();

        var participant = new TournamentParticipant
        {
            TournamentId = tournamentId,
            ParticipantId = userId
        };

        context.TournamentParticipants.Add(participant);
        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            throw new GraphQLException("Failed to join tournament due to a database error.");
        }

        return true;
    }

    [Error<TournamentNameEmptyException>]
    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Tournament>> CreateTournament(
        CreateTournamentInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        CancellationToken token)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new TournamentNameEmptyException();
        }

        var tournament = new Tournament
        {
            Name = input.Name,
            StartDate = input.StartDate,
            Status = input.Status,
            OwnerId = userId
        };

        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync(token);

        return context.Tournaments.Where(t => t.Id == tournament.Id);
    }

    [Error<TournamentNotFoundException>]
    [Error<TournamentNotOwnerException>]
    [Error<TournamentNameEmptyException>]
    [UseFirstOrDefault]
    [UseProjection]
    [Authorize]
    public async Task<IQueryable<Tournament>> UpdateTournament(
        UpdateTournamentInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        CancellationToken token)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        var tournament = await context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token)
            ?? throw new TournamentNotFoundException();

        if (tournament.OwnerId != userId)
            throw new TournamentNotOwnerException();

        if (input.Name != null)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new TournamentNameEmptyException();
            tournament.Name = input.Name;
        }

        if (input.StartDate != null)
            tournament.StartDate = input.StartDate.Value;

        if (input.Status != null)
            tournament.Status = input.Status.Value;

        await context.SaveChangesAsync(token);

        return context.Tournaments.Where(t => t.Id == tournament.Id);
    }

    [Error<TournamentNotFoundException>]
    [Authorize]
    public async Task<bool> DeleteTournament(
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
        .Include(t => t.Owner)
        .Include(t => t.Bracket)
            .ThenInclude(b => b.Matches)
        .Include(t => t.Participants)
        .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        if (tournament is null) throw new TournamentNotFoundException();

        if (tournament.OwnerId != userId)
            throw new TournamentNotOwnerException();

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

        return true;
    }
}
