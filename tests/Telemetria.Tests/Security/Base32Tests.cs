using System.Text;
using Telemetria.Security;
using Xunit;

namespace Telemetria.Tests.Security;

public sealed class Base32Tests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY======")]
    [InlineData("fo", "MZXQ====")]
    [InlineData("foo", "MZXW6===")]
    [InlineData("foob", "MZXW6YQ=")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI======")]
    public void Encode_MatchesRfc4648Vectors(string input, string expected)
    {
        var encoded = Base32.Encode(Encoding.ASCII.GetBytes(input));
        Assert.Equal(expected, encoded);
    }

    [Theory]
    [InlineData("MY======", "f")]
    [InlineData("MZXW6YTBOI======", "foobar")]
    [InlineData("mzxw6ytboi", "foobar")]
    public void Decode_MatchesRfc4648Vectors(string input, string expected)
    {
        var decoded = Base32.Decode(input);
        Assert.Equal(expected, Encoding.ASCII.GetString(decoded));
    }

    [Fact]
    public void EncodeThenDecode_RoundTrips()
    {
        var data = new byte[] { 0, 1, 2, 250, 251, 252, 253, 254, 255, 42 };
        var roundTripped = Base32.Decode(Base32.Encode(data));
        Assert.Equal(data, roundTripped);
    }

    [Fact]
    public void Decode_InvalidCharacter_Throws()
        => Assert.Throws<FormatException>(() => Base32.Decode("1111"));

    [Fact]
    public void Decode_Empty_ReturnsEmpty()
        => Assert.Empty(Base32.Decode(""));
}
