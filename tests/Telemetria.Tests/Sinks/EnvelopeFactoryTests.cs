using Microsoft.Extensions.Time.Testing;
using Telemetria.Security;
using Telemetria.Serialization;
using Telemetria.Sinks;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Sinks;

public sealed class EnvelopeFactoryTests : IDisposable
{
    private readonly EcdsaSigningKeyPair _signingKeyPair = EcdsaSigningKeyPair.Generate();
    private readonly EcdsaRequestSigner _signer;

    public EnvelopeFactoryTests()
    {
        _signer = new EcdsaRequestSigner(_signingKeyPair.PrivateKey);
    }

    public void Dispose() => _signer.Dispose();

    private static SignalBatch Batch()
        => new()
        {
            CreatedAt = DateTimeOffset.UnixEpoch.AddSeconds(60),
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = "opened" }]
        };

    private EnvelopeFactory CreateFactory(HybridKeyPair encKeyPair, IRequestSigner? signer = null)
    {
        var serializer = new JsonSignalSerializer();
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(60));
        var otp = new TotpProvider(new OneTimePasswordOptions { SecretBase32 = Base32.Encode("12345678901234567890"u8) }, clock);
        var identity = new AnonymousIdentityProvider();
        return new EnvelopeFactory(
            serializer,
            new HybridPayloadProtector(encKeyPair.PublicKey, "key-1"),
            otp,
            identity,
            signer ?? _signer,
            clock);
    }

    [Fact]
    public void Create_ProducesEnvelopeThatDecryptsToOriginalBatch()
    {
        var encKeyPair = HybridKeyPair.Generate();
        var factory = CreateFactory(encKeyPair);

        var envelope = factory.Create(Batch());

        using var opener = new HybridPayloadOpener(encKeyPair.PrivateKey);
        var serializer = new JsonSignalSerializer();
        var restored = serializer.Deserialize(opener.Open(envelope.Payload));
        Assert.Equal("opened", Assert.Single(restored.Signals).Name);
    }

    [Fact]
    public void Create_WithSigner_IncludesRequestSignature()
    {
        var factory = CreateFactory(HybridKeyPair.Generate());
        var envelope = factory.Create(Batch());

        Assert.NotNull(envelope.RequestSignature);
        Assert.NotEmpty(envelope.RequestSignature);
    }

    [Fact]
    public void Create_WithSigner_EachCallProducesDistinctSignature()
    {
        var factory = CreateFactory(HybridKeyPair.Generate());
        var env1 = factory.Create(Batch());
        var env2 = factory.Create(Batch());

        Assert.NotEqual(env1.RequestSignature, env2.RequestSignature);
    }

    [Fact]
    public void Create_WithNoopSigner_HasNullRequestSignature()
    {
        var factory = CreateFactory(HybridKeyPair.Generate(), NoopRequestSigner.Instance);
        var envelope = factory.Create(Batch());

        Assert.Null(envelope.RequestSignature);
    }

    [Fact]
    public void Create_NullBatch_Throws()
    {
        var factory = CreateFactory(HybridKeyPair.Generate());
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }
}
