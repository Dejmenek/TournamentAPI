using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Inputs;
using TournamentAPI.Models;
using TournamentAPI.Services;

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

        tournament.IsDeleted = true;
        if (tournament.Bracket != null)
        {
            tournament.Bracket.IsDeleted = true;
            foreach (var match in tournament.Bracket.Matches)
            {
                match.IsDeleted = true;
            }
        }

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

    public async Task<Bracket> UpdateRound(int bracketId, int roundNumber, [Service] ApplicationDbContext context)
    {
        var bracket = await context.Brackets
            .Include(b => b.Matches)
            .ThenInclude(m => m.Winner)
            .FirstOrDefaultAsync(b => b.Id == bracketId)
            ?? throw new GraphQLException("Bracket doesn't exist");

        var matchesInRound = bracket.Matches.Where(m => m.Round == roundNumber).ToList();
        if (matchesInRound.Any(m => m.WinnerId == null))
            throw new GraphQLException("Not all matches in the current round have been played.");

        var winners = matchesInRound.Select(m => m.Winner!).ToList();
        for (int i = 0; i < winners.Count; i += 2)
        {
            Match match = new()
            {
                Round = roundNumber + 1,
                Player1Id = winners[i].Id,
                Player2Id = (i + 1 < winners.Count) ? winners[i + 1].Id : null,
                BracketId = bracket.Id,
            };
            bracket.Matches.Add(match);
        }

        await context.SaveChangesAsync();

        return bracket;
    }

    public async Task<bool> Play(int matchId, int winnerId, [Service] ApplicationDbContext context)
    {
        var match = await context.Matches
            .Include(m => m.Bracket)
            .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new GraphQLException("Match doesn't exist");

        var tournament = match.Bracket.Tournament;

        if (tournament.Status != TournamentStatus.Closed)
            throw new GraphQLException("Matches can only be played when the tournament is closed.");

        if (match.WinnerId != null)
            throw new GraphQLException("Match has already been played.");

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
            throw new GraphQLException("Winner must be one of the match participants.");

        match.WinnerId = winnerId;
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RegisterUser(RegisterUserInput input, [Service] UserManager<ApplicationUser> userManager)
    {
        var user = new ApplicationUser
        {
            UserName = input.UserName,
            Email = input.Email
        };

        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new GraphQLException($"User registration failed: {errors}");
        }

        return true;
    }

    public async Task<string> LoginUser(LoginUserInput input, [Service] UserManager<ApplicationUser> userManager, [Service] JwtService jwtService)
    {
        var user = await userManager.FindByEmailAsync(input.Email)
            ?? throw new GraphQLException("Invalid username or password.");

        if (!await userManager.CheckPasswordAsync(user, input.Password))
            throw new GraphQLException("Invalid username or password.");

        var token = jwtService.CreateToken(user);
        return token;
    }
}
