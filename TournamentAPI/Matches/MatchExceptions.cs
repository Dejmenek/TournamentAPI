namespace TournamentAPI.Matches;

public sealed class MatchNotFoundException() : Exception($"Match was not found.");
public sealed class MatchAlreadyPlayedException() : Exception($"Match has already been played.");
public sealed class InvalidMatchWinnerException() : Exception($"Winner must be one of the match participants.");
public sealed class TournamentNotClosedException() : Exception($"Matches can only be played when the tournament is closed.");
