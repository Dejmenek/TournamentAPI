namespace TournamentAPI.Tournaments;

public sealed class TournamentNotFoundException() : Exception("Tournament doesn't exist");
public sealed class TournamentClosedException() : Exception("Tournament is closed");
public sealed class UserAlreadyParticipantException() : Exception("User already participates in the tournament");
public sealed class TournamentJoinFailedException() : Exception("Failed to join tournament due to a database error");
public sealed class TournamentNameEmptyException() : Exception("Tournament name cannot be empty");
public sealed class TournamentNotOwnerException() : Exception("User is not the owner of the tournament");
