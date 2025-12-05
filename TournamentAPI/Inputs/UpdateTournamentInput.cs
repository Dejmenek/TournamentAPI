using TournamentAPI.Models;

namespace TournamentAPI.Inputs;

public record UpdateTournamentInput(string? Name, DateTime? StartDate, TournamentStatus? Status);
