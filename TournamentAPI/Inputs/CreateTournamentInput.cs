using TournamentAPI.Models;

namespace TournamentAPI.Inputs;

public record CreateTournamentInput(string Name, DateTime StartDate, TournamentStatus Status);
