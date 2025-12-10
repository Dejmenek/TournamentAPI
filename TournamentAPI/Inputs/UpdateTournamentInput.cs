using TournamentAPI.Models;

namespace TournamentAPI.Inputs;

public record UpdateTournamentInput(int TournamentId, string? Name, DateTime? StartDate, TournamentStatus? Status);
