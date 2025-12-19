namespace TournamentAPI.Brackets;

public sealed class BracketAlreadyExistsException() : Exception("Bracket already exists for this tournament.");
public sealed class NotEnoughParticipantsException() : Exception("Not enough participants to create a bracket");
public sealed class BracketNotFoundException() : Exception("Bracket doesn't exist");
public sealed class NoMatchesInRoundException() : Exception("No matches found in the specified round.");
public sealed class NotAllMatchesPlayedException() : Exception("Not all matches in the current round have been played.");
public sealed class BracketGenerationNotAllowedException() : Exception("Bracket can only be generated when the tournament is closed.");
