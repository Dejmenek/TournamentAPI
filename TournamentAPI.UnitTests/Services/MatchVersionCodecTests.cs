using TournamentAPI.Matches;

namespace TournamentAPI.UnitTests.Services;

public class MatchVersionCodecTests
{
    [Fact]
    public void EncodeThenTryDecode_RoundTripsToTheOriginalBytes()
    {
        byte[] rowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

        var encoded = MatchVersionCodec.Encode(rowVersion);
        var decoded = MatchVersionCodec.TryDecode(encoded, out var result);

        Assert.True(decoded);
        Assert.Equal(rowVersion, result);
    }

    [Fact]
    public void TryDecode_WhenInputIsNull_ReturnsFalse()
    {
        var decoded = MatchVersionCodec.TryDecode(null, out var result);

        Assert.False(decoded);
        Assert.Empty(result);
    }

    [Fact]
    public void TryDecode_WhenInputIsEmpty_ReturnsFalse()
    {
        var decoded = MatchVersionCodec.TryDecode(string.Empty, out var result);

        Assert.False(decoded);
        Assert.Empty(result);
    }

    [Fact]
    public void TryDecode_WhenInputIsMalformedBase64_ReturnsFalse()
    {
        var decoded = MatchVersionCodec.TryDecode("not-valid-base64!!", out var result);

        Assert.False(decoded);
        Assert.Empty(result);
    }
}
