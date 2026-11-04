using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public record CreateTournamentInput(
    string Name,
    DateTime StartDate,
    TournamentStatus Status,
    int MaxParticipants,
    TournamentFormat? Format = null,
    long EntryFeeAmount = 0,
    IReadOnlyList<PrizeInput>? Prizes = null);
