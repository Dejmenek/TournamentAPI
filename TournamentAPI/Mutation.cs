using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Inputs;
using TournamentAPI.Models;

namespace TournamentAPI;

public class Mutation
{
    public async Task<Tournament> AddParticipant(int userId, int tournamentId, [Service] ApplicationDbContext context)
    {
        var tournament = await context.Tournaments
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == tournamentId)
            ?? throw new GraphQLException("Tournament doesn't exist");

        if (tournament.Status == "Closed") throw new GraphQLException("Tournament is closed");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new GraphQLException("User doesn't exist");
        if (tournament.Participants.Contains(user)) throw new GraphQLException("User already participates in the tournament");

        tournament.Participants.Add(user);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new GraphQLException("Failed to add participant due to a database error.");
        }

        return tournament;
    }

    public async Task<Tournament> CreateTournament(TournamentInput input, [Service] ApplicationDbContext context)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new GraphQLException("Tournament name cannot be empty.");
        }

        var tournament = new Tournament
        {
            Name = input.Name,
            StartDate = input.StartDate,
            Status = input.Status
        };

        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync();

        return tournament;
    }
}
