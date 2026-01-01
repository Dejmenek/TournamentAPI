namespace TournamentAPI.Brackets;

public static class BracketErrorCodes
{
    public const string BracketAlreadyExists = "Bracket.AlreadyExists";
    public const string NotEnoughParticipants = "Bracket.NotEnoughParticipants";
    public const string BracketNotFound = "Bracket.NotFound";
    public const string BracketNotFoundInTournament = "Bracket.NotFoundInTournament";
    public const string NoMatchesInRound = "Bracket.NoMatchesInRound";
    public const string NotAllMatchesPlayed = "Bracket.NotAllMatchesPlayed";
    public const string BracketGenerationNotAllowed = "Bracket.GenerationNotAllowed";
    public const string BracketAlreadyHasWinner = "Bracket.AlreadyHasWinner";
    public const string NextRoundAlreadyGenerated = "Bracket.NextRoundAlreadyGenerated";
}
