using System.Text;
using Microsoft.Extensions.Time.Testing;
using Telemetria.Security;
using Xunit;

namespace Telemetria.Tests.Security;

public sealed class TotpProviderTests
{
    private static readonly byte[] Sha1Secret = Encoding.ASCII.GetBytes("12345678901234567890");
    private static readonly byte[] Sha256Secret = Encoding.ASCII.GetBytes("12345678901234567890123456789012");
    private static readonly byte[] Sha512Secret = Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234");

    private static TotpProvider Create(byte[] secret, OtpAlgorithm algorithm, int digits = 8, int window = 1)
        => new(secret, digits, TimeSpan.FromSeconds(30), algorithm, window, new FakeTimeProvider());

    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1111111111L, "14050471")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(2000000000L, "69279037")]
    [InlineData(20000000000L, "65353130")]
    public void GenerateAt_Sha1_MatchesRfc6238Vectors(long unixSeconds, string expected)
    {
        var provider = Create(Sha1Secret, OtpAlgorithm.Sha1);
        var code = provider.GenerateAt(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));
        Assert.Equal(expected, code);
    }

    [Fact]
    public void GenerateAt_Sha256_MatchesRfc6238Vector()
    {
        var provider = Create(Sha256Secret, OtpAlgorithm.Sha256);
        Assert.Equal("46119246", provider.GenerateAt(DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    [Fact]
    public void GenerateAt_Sha512_MatchesRfc6238Vector()
    {
        var provider = Create(Sha512Secret, OtpAlgorithm.Sha512);
        Assert.Equal("90693936", provider.GenerateAt(DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    [Fact]
    public void Generate_UsesProvidedTimeProvider()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));
        var provider = new TotpProvider(Sha1Secret, 8, TimeSpan.FromSeconds(30), OtpAlgorithm.Sha1, 1, clock);
        Assert.Equal("94287082", provider.Generate());
    }

    [Fact]
    public void Validate_AcceptsCurrentCode()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59));
        var provider = new TotpProvider(Sha1Secret, 8, TimeSpan.FromSeconds(30), OtpAlgorithm.Sha1, 1, clock);
        Assert.True(provider.Validate("94287082"));
    }

    [Fact]
    public void Validate_AcceptsCodeWithinWindow()
    {
        var provider = Create(Sha1Secret, OtpAlgorithm.Sha1, window: 1);
        var previous = provider.GenerateAt(DateTimeOffset.FromUnixTimeSeconds(59 - 30));
        Assert.True(provider.Validate(previous, DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    [Fact]
    public void Validate_RejectsCodeOutsideWindow()
    {
        var provider = Create(Sha1Secret, OtpAlgorithm.Sha1, window: 0);
        var previous = provider.GenerateAt(DateTimeOffset.FromUnixTimeSeconds(59 - 30));
        Assert.False(provider.Validate(previous, DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    [Fact]
    public void Validate_RejectsEmptyAndWrongCode()
    {
        var provider = Create(Sha1Secret, OtpAlgorithm.Sha1);
        Assert.False(provider.Validate(string.Empty, DateTimeOffset.FromUnixTimeSeconds(59)));
        Assert.False(provider.Validate("00000000", DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Generate_ProducesRequestedDigitCount(int digits)
    {
        var provider = Create(Sha1Secret, OtpAlgorithm.Sha1, digits);
        var code = provider.GenerateAt(DateTimeOffset.FromUnixTimeSeconds(59));
        Assert.Equal(digits, code.Length);
    }

    [Fact]
    public void Constructor_FromOptions_RejectsInvalidDigits()
    {
        var options = new OneTimePasswordOptions { Digits = 4, SecretBase32 = Base32.Encode(Sha1Secret) };
        Assert.Throws<ArgumentOutOfRangeException>(() => new TotpProvider(options, new FakeTimeProvider()));
    }

    [Fact]
    public void Constructor_FromOptions_GeneratesSecretWhenMissing()
    {
        var options = new OneTimePasswordOptions { SecretBase32 = null };
        var provider = new TotpProvider(options, new FakeTimeProvider());
        Assert.Equal(6, provider.Generate().Length);
    }
}
