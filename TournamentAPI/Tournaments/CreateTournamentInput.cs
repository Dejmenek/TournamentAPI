using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public record CreateTournamentInput(string Name, DateTime StartDate, TournamentStatus Status);
