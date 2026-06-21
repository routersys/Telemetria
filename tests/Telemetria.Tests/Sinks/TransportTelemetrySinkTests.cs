using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Telemetria.Security;
using Telemetria.Serialization;
using Telemetria.Sinks;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Sinks;

public sealed class TransportTelemetrySinkTests
{
    private static SignalBatch Batch()
        => new()
        {
            CreatedAt = DateTimeOffset.UnixEpoch,
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = "s" }]
        };

    private static EnvelopeFactory Factory()
    {
        var keyPair = HybridKeyPair.Generate();
        var clock = new FakeTimeProvider();
        return new EnvelopeFactory(
            new JsonSignalSerializer(),
            new HybridPayloadProtector(keyPair.PublicKey, "key-1"),
            new TotpProvider(new OneTimePasswordOptions(), clock),
            new AnonymousIdentityProvider(),
            NoopRequestSigner.Instance,
            clock);
    }

    [Fact]
    public async Task ExportAsync_TransportAccepts_ReturnsSuccess()
    {
        var transport = new StubTransport(_ => true);
        var sink = new TransportTelemetrySink(Factory(), transport, NullLogger<TransportTelemetrySink>.Instance);

        var result = await sink.ExportAsync(Batch());

        Assert.True(result.Succeeded);
        Assert.Single(transport.Sent);
    }

    [Fact]
    public async Task ExportAsync_TransportRejects_ReturnsFailure()
    {
        var transport = new StubTransport(_ => false);
        var sink = new TransportTelemetrySink(Factory(), transport, NullLogger<TransportTelemetrySink>.Instance);

        var result = await sink.ExportAsync(Batch());

        Assert.False(result.Succeeded);
        Assert.Equal("transport-rejected", result.Detail);
    }

    [Fact]
    public async Task ExportAsync_TransportThrows_ReturnsFailure()
    {
        var transport = new StubTransport(_ => throw new InvalidOperationException("boom"));
        var sink = new TransportTelemetrySink(Factory(), transport, NullLogger<TransportTelemetrySink>.Instance);

        var result = await sink.ExportAsync(Batch());

        Assert.False(result.Succeeded);
    }
}
