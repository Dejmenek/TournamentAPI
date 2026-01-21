namespace TournamentAPI.Brackets;

public static class BracketErrors
{
    public static IError BracketAlreadyExistsForTournament(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Bracket already exists for this tournament.")
            .SetCode(BracketErrorCodes.BracketAlreadyExists)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError NotEnoughParticipants(int participantCount) =>
        ErrorBuilder.New()
            .SetMessage("Not enough participants to create a bracket.")
            .SetCode(BracketErrorCodes.NotEnoughParticipants)
            .SetExtension("ParticipantCount", participantCount)
            .Build();

    public static IError BracketNotFound(int bracketId) =>
        ErrorBuilder.New()
            .SetMessage("Bracket doesn't exist.")
            .SetCode(BracketErrorCodes.BracketNotFound)
            .SetExtension("BracketId", bracketId)
            .Build();

    public static IError NoMatchesInRound(int roundNumber) =>
        ErrorBuilder.New()
            .SetMessage("No matches found in the specified round.")
            .SetCode(BracketErrorCodes.NoMatchesInRound)
            .SetExtension("RoundNumber", roundNumber)
            .Build();

    public static IError NotAllMatchesPlayed(int roundNumber) =>
        ErrorBuilder.New()
            .SetMessage("Not all matches in the current round have been played.")
            .SetCode(BracketErrorCodes.NotAllMatchesPlayed)
            .SetExtension("RoundNumber", roundNumber)
            .Build();

    public static IError BracketGenerationNotAllowed(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Bracket can only be generated when the tournament is closed.")
            .SetCode(BracketErrorCodes.BracketGenerationNotAllowed)
            .SetExtension("TournamentId", tournamentId)
            .Build();

    public static IError BracketAlreadyHasWinner(int bracketId) =>
        ErrorBuilder.New()
            .SetMessage("Bracket already has a winner.")
            .SetCode(BracketErrorCodes.BracketAlreadyHasWinner)
            .SetExtension("BracketId", bracketId)
            .Build();

    public static IError NextRoundAlreadyGenerated(int bracketId) =>
        ErrorBuilder.New()
            .SetMessage("Next round has already been generated.")
            .SetCode(BracketErrorCodes.NextRoundAlreadyGenerated)
            .SetExtension("BracketId", bracketId)
            .Build();
}
