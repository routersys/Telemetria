using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Telemetria.Buffering;
using Telemetria.Collection;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Collection;

public sealed class TelemetryClientTests
{
    private sealed class NoopExporter : ISignalExporter
    {
        public ValueTask<int> PumpAsync(CancellationToken cancellationToken) => ValueTask.FromResult(0);

        public int FlushCount { get; private set; }

        public ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            FlushCount++;
            return ValueTask.CompletedTask;
        }
    }

    private static (TelemetryClient Client, ChannelSignalBuffer Buffer, NoopExporter Exporter, FakeTimeProvider Clock) Create(
        TelemetriaOptions? options = null,
        int capacity = 16)
    {
        var buffer = new ChannelSignalBuffer(Options.Create(new BufferOptions { Capacity = capacity }));
        var exporter = new NoopExporter();
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(1000));
        var monitor = new StaticOptionsMonitor<TelemetriaOptions>(options ?? new TelemetriaOptions());
        var client = new TelemetryClient(buffer, exporter, clock, monitor, NullLogger<TelemetryClient>.Instance);
        return (client, buffer, exporter, clock);
    }

    [Fact]
    public void TrackUsage_WritesUsageSignal()
    {
        var (client, buffer, _, clock) = Create();
        client.TrackUsage("opened", new Dictionary<string, string> { ["doc"] = "1" });

        Assert.True(buffer.TryReadBatch(10, out var batch));
        var signal = Assert.Single(batch);
        Assert.Equal(SignalCategory.Usage, signal.Category);
        Assert.Equal("opened", signal.Name);
        Assert.Equal("1", signal.Properties["doc"]);
        Assert.Equal(clock.GetUtcNow(), signal.Timestamp);
    }

    [Fact]
    public void TrackMetric_StoresValueMeasurement()
    {
        var (client, buffer, _, _) = Create();
        client.TrackMetric("latency", 42.0);

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal(42.0, batch[0].Measurements["value"]);
    }

    [Fact]
    public void TrackException_BuildsSnapshot()
    {
        var (client, buffer, _, _) = Create();
        client.TrackException(new InvalidOperationException("boom"));

        Assert.True(buffer.TryReadBatch(10, out var batch));
        var signal = batch[0];
        Assert.Equal(SignalCategory.Exception, signal.Category);
        Assert.Equal("boom", signal.Exception!.Message);
    }

    [Fact]
    public void TrackError_UsesProvidedSeverity()
    {
        var (client, buffer, _, _) = Create();
        client.TrackError("failure", SignalSeverity.Critical);

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal(SignalSeverity.Critical, batch[0].Severity);
    }

    [Fact]
    public void Track_WhenDisabled_DoesNothing()
    {
        var (client, buffer, _, _) = Create(new TelemetriaOptions { Mode = TelemetriaMode.Disabled });
        client.TrackUsage("ignored");
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Track_WhenBufferFull_DropsWithoutThrowing()
    {
        var (client, buffer, _, _) = Create(new TelemetriaOptions { Mode = TelemetriaMode.Local }, capacity: 1);
        client.TrackUsage("a");
        client.TrackUsage("b");
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void Track_PreservesExplicitTimestamp()
    {
        var (client, buffer, _, _) = Create();
        var stamp = DateTimeOffset.UnixEpoch.AddSeconds(12345);
        client.Track(new TelemetrySignal { Category = SignalCategory.Custom, Name = "c", Timestamp = stamp });

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal(stamp, batch[0].Timestamp);
    }

    [Fact]
    public async Task FlushAsync_DelegatesToExporter()
    {
        var (client, _, exporter, _) = Create();
        await client.FlushAsync();
        Assert.Equal(1, exporter.FlushCount);
    }

    [Fact]
    public void TrackUsage_NullName_Throws()
    {
        var (client, _, _, _) = Create();
        Assert.Throws<ArgumentNullException>(() => client.TrackUsage(null!));
    }
}
