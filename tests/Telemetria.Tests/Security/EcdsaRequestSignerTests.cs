using Telemetria.Security;
using Xunit;

namespace Telemetria.Tests.Security;

public sealed class EcdsaRequestSignerTests
{
    private static (EcdsaRequestSigner Signer, EcdsaSigningKeyPair KeyPair) Create()
    {
        var kp = EcdsaSigningKeyPair.Generate();
        return (new EcdsaRequestSigner(kp.PrivateKey), kp);
    }

    [Fact]
    public void Sign_ProducesNonEmptyBase64()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var sig = signer.Sign("hello"u8);
            Assert.NotEmpty(sig);
            Assert.True(IsValidBase64(sig));
        }
    }

    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var payload = "payload"u8;
            var sig = signer.Sign(payload);
            Assert.True(signer.Verify(payload, sig));
        }
    }

    [Fact]
    public void Verify_TamperedPayload_ReturnsFalse()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var sig = signer.Sign("original"u8);
            Assert.False(signer.Verify("tampered"u8, sig));
        }
    }

    [Fact]
    public void Verify_ZeroedSignatureBytes_ReturnsFalse()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var payload = "data"u8;
            signer.Sign(payload);
            Assert.False(signer.Verify(payload, Convert.ToBase64String(new byte[64])));
        }
    }

    [Fact]
    public void Verify_InvalidBase64_ReturnsFalse()
    {
        var (signer, _) = Create();
        using (signer)
        {
            Assert.False(signer.Verify("data"u8, "not-valid-base64!!!"));
        }
    }

    [Fact]
    public void Verify_NullSignature_Throws()
    {
        var (signer, _) = Create();
        using (signer)
        {
            Assert.Throws<ArgumentNullException>(() => signer.Verify("data"u8, null!));
        }
    }

    [Fact]
    public void Sign_SamePayload_DifferentSignaturesEachTime()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var payload = "same"u8;
            var sig1 = signer.Sign(payload);
            var sig2 = signer.Sign(payload);
            Assert.NotEqual(sig1, sig2);
        }
    }

    [Fact]
    public void Sign_DifferentPayloads_SignaturesAreDistinct()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var sig1 = signer.Sign("payload-a"u8);
            var sig2 = signer.Sign("payload-b"u8);
            Assert.NotEqual(sig1, sig2);
        }
    }

    [Fact]
    public void Sign_FromDifferentKey_FailsVerificationOnOtherSigner()
    {
        var (signer1, _) = Create();
        var (signer2, _) = Create();
        using (signer1)
        using (signer2)
        {
            var payload = "data"u8;
            var sig = signer1.Sign(payload);
            Assert.False(signer2.Verify(payload, sig));
        }
    }

    [Fact]
    public void Verify_EmptyPayload_RoundTrips()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var sig = signer.Sign(ReadOnlySpan<byte>.Empty);
            Assert.True(signer.Verify(ReadOnlySpan<byte>.Empty, sig));
        }
    }

    [Fact]
    public void Constructor_NullKey_Throws()
        => Assert.Throws<ArgumentNullException>(() => new EcdsaRequestSigner(null!));

    [Fact]
    public void Constructor_InvalidKey_Throws()
        => Assert.Throws<System.Security.Cryptography.CryptographicException>(
            () => new EcdsaRequestSigner([1, 2, 3]));

    [Fact]
    public void EcdsaSigningKeyPair_Generate_ProducesNonEmptyKeys()
    {
        var kp = EcdsaSigningKeyPair.Generate();
        Assert.NotEmpty(kp.PrivateKey);
        Assert.NotEmpty(kp.PublicKey);
        Assert.NotEmpty(kp.PrivateKeyBase64);
        Assert.NotEmpty(kp.PublicKeyBase64);
    }

    [Fact]
    public void EcdsaSigningKeyPair_Base64_DecodesBackToOriginalBytes()
    {
        var kp = EcdsaSigningKeyPair.Generate();
        Assert.Equal(kp.PrivateKey, Convert.FromBase64String(kp.PrivateKeyBase64));
        Assert.Equal(kp.PublicKey, Convert.FromBase64String(kp.PublicKeyBase64));
    }

    [Fact]
    public void EcdsaSigningKeyPair_TwoGenerations_ProduceUniqueKeys()
    {
        var kp1 = EcdsaSigningKeyPair.Generate();
        var kp2 = EcdsaSigningKeyPair.Generate();
        Assert.NotEqual(kp1.PrivateKeyBase64, kp2.PrivateKeyBase64);
        Assert.NotEqual(kp1.PublicKeyBase64, kp2.PublicKeyBase64);
    }

    [Fact]
    public void Sign_LargePayload_RoundTrips()
    {
        var (signer, _) = Create();
        using (signer)
        {
            var payload = new byte[64 * 1024];
            Random.Shared.NextBytes(payload);
            var sig = signer.Sign(payload);
            Assert.True(signer.Verify(payload, sig));
        }
    }

    private static bool IsValidBase64(string s)
    {
        var buffer = new byte[((s.Length + 3) / 4) * 3];
        return Convert.TryFromBase64String(s, buffer, out _);
    }
}
