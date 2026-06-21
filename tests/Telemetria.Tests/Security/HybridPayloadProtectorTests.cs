using System.Security.Cryptography;
using System.Text;
using Telemetria.Security;
using Xunit;

namespace Telemetria.Tests.Security;

public sealed class HybridPayloadProtectorTests
{
    private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("テレメトリの機密ペイロード payload-123");

    [Fact]
    public void ProtectThenOpen_RoundTrips()
    {
        var keyPair = HybridKeyPair.Generate();
        var protector = new HybridPayloadProtector(keyPair.PublicKey, "key-1");
        using var opener = new HybridPayloadOpener(keyPair.PrivateKey);

        var payload = protector.Protect(Plaintext);
        var recovered = opener.Open(payload);

        Assert.Equal(Plaintext, recovered);
        Assert.Equal("key-1", payload.KeyId);
    }

    [Fact]
    public void Protect_ProducesUniqueNonceAndEphemeralKeyEachTime()
    {
        var keyPair = HybridKeyPair.Generate();
        var protector = new HybridPayloadProtector(keyPair.PublicKey, "key-1");

        var first = protector.Protect(Plaintext);
        var second = protector.Protect(Plaintext);

        Assert.NotEqual(first.Nonce, second.Nonce);
        Assert.NotEqual(first.EphemeralPublicKey, second.EphemeralPublicKey);
        Assert.NotEqual(first.CipherText, second.CipherText);
    }

    [Fact]
    public void Open_TamperedTag_Throws()
    {
        var keyPair = HybridKeyPair.Generate();
        var protector = new HybridPayloadProtector(keyPair.PublicKey, "key-1");
        using var opener = new HybridPayloadOpener(keyPair.PrivateKey);

        var payload = protector.Protect(Plaintext);
        payload.Tag[0] ^= 0xFF;

        Assert.Throws<AuthenticationTagMismatchException>(() => opener.Open(payload));
    }

    [Fact]
    public void Open_TamperedCipherText_Throws()
    {
        var keyPair = HybridKeyPair.Generate();
        var protector = new HybridPayloadProtector(keyPair.PublicKey, "key-1");
        using var opener = new HybridPayloadOpener(keyPair.PrivateKey);

        var payload = protector.Protect(Plaintext);
        payload.CipherText[0] ^= 0xFF;

        Assert.Throws<AuthenticationTagMismatchException>(() => opener.Open(payload));
    }

    [Fact]
    public void Open_WrongPrivateKey_Throws()
    {
        var keyPair = HybridKeyPair.Generate();
        var otherKeyPair = HybridKeyPair.Generate();
        var protector = new HybridPayloadProtector(keyPair.PublicKey, "key-1");
        using var opener = new HybridPayloadOpener(otherKeyPair.PrivateKey);

        var payload = protector.Protect(Plaintext);

        Assert.Throws<AuthenticationTagMismatchException>(() => opener.Open(payload));
    }

    [Fact]
    public void Protect_EmptyPlaintext_RoundTrips()
    {
        var keyPair = HybridKeyPair.Generate();
        var protector = new HybridPayloadProtector(keyPair.PublicKey, "key-1");
        using var opener = new HybridPayloadOpener(keyPair.PrivateKey);

        var payload = protector.Protect(ReadOnlySpan<byte>.Empty);
        Assert.Empty(opener.Open(payload));
    }
}
