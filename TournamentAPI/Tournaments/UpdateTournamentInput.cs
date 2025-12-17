using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public record UpdateTournamentInput(int TournamentId, string? Name, DateTime? StartDate, TournamentStatus? Status);
