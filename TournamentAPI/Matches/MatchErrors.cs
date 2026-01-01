namespace TournamentAPI.Matches;

public static class MatchErrors
{
    public static IError MatchNotFound(int matchId) =>
        ErrorBuilder.New()
            .SetMessage("Match was not found.")
            .SetCode(MatchErrorCodes.MatchNotFound)
            .SetExtension("MatchId", matchId)
            .Build();

    public static IError MatchAlreadyPlayed(int matchId) =>
        ErrorBuilder.New()
            .SetMessage("Match has already been played.")
            .SetCode(MatchErrorCodes.MatchAlreadyPlayed)
            .SetExtension("MatchId", matchId)
            .Build();

    public static IError InvalidMatchWinner(int matchId, int winnerId) =>
        ErrorBuilder.New()
            .SetMessage("Winner must be one of the match participants.")
            .SetCode(MatchErrorCodes.InvalidMatchWinner)
            .SetExtension("MatchId", matchId)
            .SetExtension("WinnerId", winnerId)
            .Build();

    public static IError TournamentNotClosed(int tournamentId) =>
        ErrorBuilder.New()
            .SetMessage("Matches can only be played when the tournament is closed.")
            .SetCode(MatchErrorCodes.TournamentNotClosed)
            .SetExtension("TournamentId", tournamentId)
            .Build();
}
