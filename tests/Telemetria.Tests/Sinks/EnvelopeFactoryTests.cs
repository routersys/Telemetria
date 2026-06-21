using Microsoft.Extensions.Time.Testing;
using Telemetria.Security;
using Telemetria.Serialization;
using Telemetria.Sinks;
using Xunit;

namespace Telemetria.Tests.Sinks;

public sealed class EnvelopeFactoryTests
{
    private static SignalBatch Batch()
        => new()
        {
            CreatedAt = DateTimeOffset.UnixEpoch.AddSeconds(60),
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = "opened" }]
        };

    [Fact]
    public void Create_ProducesEnvelopeThatDecryptsToOriginalBatch()
    {
        var keyPair = HybridKeyPair.Generate();
        var serializer = new JsonSignalSerializer();
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(60));
        var otp = new TotpProvider(new OneTimePasswordOptions { SecretBase32 = Base32.Encode("12345678901234567890"u8) }, clock);
        var identity = new AnonymousIdentityProvider();

        var factory = new EnvelopeFactory(
            serializer,
            new HybridPayloadProtector(keyPair.PublicKey, "key-1"),
            otp,
            identity,
            clock);

        var envelope = factory.Create(Batch());

        Assert.Equal(identity.Current, envelope.AnonymousId);
        Assert.True(otp.Validate(envelope.OneTimePassword, clock.GetUtcNow()));

        using var opener = new HybridPayloadOpener(keyPair.PrivateKey);
        var restored = serializer.Deserialize(opener.Open(envelope.Payload));
        Assert.Equal("opened", Assert.Single(restored.Signals).Name);
    }
}
