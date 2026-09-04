namespace TournamentAPI.Tournaments;

public static class TournamentErrorCodes
{
    public const string TournamentNotFound = "Tournament.NotFound";
    public const string TournamentClosed = "Tournament.Closed";
    public const string UserAlreadyParticipant = "Tournament.UserAlreadyParticipant";
    public const string TournamentNameEmpty = "Tournament.NameEmpty";
    public const string TournamentNotOwner = "Tournament.NotOwner";
    public const string TournamentFull = "Tournament.Full";
    public const string InvalidMaxParticipants = "Tournament.InvalidMaxParticipants";
    public const string MaxParticipantsBelowParticipantCount = "Tournament.MaxParticipantsBelowParticipantCount";
}
