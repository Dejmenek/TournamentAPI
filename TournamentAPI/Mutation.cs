using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Data;
using TournamentAPI.Inputs;
using TournamentAPI.Models;
using TournamentAPI.Services;

namespace TournamentAPI;

public class Mutation
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public Mutation(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Tournament> AddParticipant(AddParticipantInput input, [Service] ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var tournament = await context.Tournaments
        .Include(t => t.Participants)
        .FirstOrDefaultAsync(t => t.Id == input.TournamentId)
        ?? throw new GraphQLException("Tournament doesn't exist");

        if (tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can add participants.");

        if (tournament.Status == TournamentStatus.Closed)
            throw new GraphQLException("Tournament is closed");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == input.UserId)
            ?? throw new GraphQLException("User doesn't exist");

        bool alreadyParticipates = tournament.Participants.Any(tp => tp.ParticipantId == input.UserId);
        if (alreadyParticipates)
            throw new GraphQLException("User already participates in the tournament");

        var participant = new TournamentParticipant
        {
            TournamentId = input.TournamentId,
            ParticipantId = input.UserId
        };

        context.TournamentParticipants.Add(participant);

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

    public async Task<bool> JoinTournament(int tournamentId, [Service] ClaimsPrincipal userClaims)
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

    public async Task<Tournament> CreateTournament(CreateTournamentInput input, [Service] ClaimsPrincipal userClaims)
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

    public async Task<Tournament> UpdateTournament(UpdateTournamentInput input, [Service] ClaimsPrincipal userClaims)
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

    public async Task<bool> DeleteTournament(int tournamentId, [Service] ClaimsPrincipal userClaims)
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

    public async Task<Bracket> GenerateBracket(int tournamentId, [Service] ClaimsPrincipal userClaims)
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

        for (int i = 0; i < participants.Count; i++)
        {
            Match match = new()
            {
                Round = 1,
                Player1Id = participants[i].ParticipantId,
                Player2Id = (i + 1 < participants.Count) ? participants[i + 1].ParticipantId : null,
                Bracket = bracket,
            };

            bracket.Matches.Add(match);
        }

        context.Brackets.Add(bracket);
        tournament.Bracket = bracket;

        await context.SaveChangesAsync();

        return bracket;
    }

    public async Task<Bracket> UpdateRound(int bracketId, int roundNumber, [Service] ClaimsPrincipal userClaims)
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

    public async Task<bool> Play(int matchId, int winnerId, [Service] ClaimsPrincipal userClaims)
    {
        var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("User is not authenticated.");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            throw new GraphQLException("Invalid user ID.");

        using var context = _contextFactory.CreateDbContext();

        var match = await context.Matches
            .Include(m => m.Bracket)
                .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new GraphQLException("Match doesn't exist");

        var tournament = match.Bracket.Tournament;

        if (tournament.OwnerId != userId)
            throw new GraphQLException("Only the tournament owner can record match results.");

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

    public async Task<string> LoginUser(
        LoginUserInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] JwtService jwtService)
    {
        var user = await userManager.FindByEmailAsync(input.Email)
            ?? throw new GraphQLException("Invalid username or password.");

        if (!await userManager.CheckPasswordAsync(user, input.Password))
            throw new GraphQLException("Invalid username or password.");

        var token = jwtService.CreateToken(user);
        return token;
    }
}
