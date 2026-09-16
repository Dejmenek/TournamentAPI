namespace TournamentAPI.Matches;

public static class MatchVersionCodec
{
    public static string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static bool TryDecode(string? version, out byte[] rowVersion)
    {
        if (string.IsNullOrEmpty(version))
        {
            rowVersion = [];
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(version);
            return true;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }
}
