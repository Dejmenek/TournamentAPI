using TournamentAPI.Models;

namespace TournamentAPI.Inputs;

public record TournamentInput(string Name, DateTime StartDate, TournamentStatus Status);
