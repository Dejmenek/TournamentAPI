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

        if (tournament.Status == TournamentStatus.Closed) throw new GraphQLException("Tournament is closed");

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

    public async Task<Tournament> CreateTournament(CreateTournamentInput input, [Service] ApplicationDbContext context)
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

    public async Task<Tournament> UpdateTournament(int tournamentId, UpdateTournamentInput input, [Service] ApplicationDbContext context)
    {
        var tournament = await context.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId)
            ?? throw new GraphQLException("Tournament doesn't exist");

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

    public async Task<bool> DeleteTournament(int tournamentId, [Service] ApplicationDbContext context)
    {
        var tournament = await context.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament is null) return false;

        context.Tournaments.Remove(tournament);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<Bracket> GenerateBracket(int tournamentId, [Service] ApplicationDbContext context)
    {
        var tournament = await context.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Bracket)
            .FirstOrDefaultAsync(t => t.Id == tournamentId)
            ?? throw new GraphQLException("Tournament doesn't exist");

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

        for (int i = 0; i < participants.Count; i++)
        {
            Match match = new()
            {
                Round = 1,
                Player1Id = participants[i].Id,
                Player2Id = (i + 1 < participants.Count) ? participants[i + 1].Id : null,
                Bracket = bracket,
            };

            bracket.Matches.Add(match);
        }

        context.Brackets.Add(bracket);
        tournament.Bracket = bracket;

        await context.SaveChangesAsync();

        return bracket;
    }
}
