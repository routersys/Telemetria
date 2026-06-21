using Telemetria.Sinks;
using Xunit;

namespace Telemetria.Tests.Sinks;

public sealed class InMemoryTelemetrySinkTests
{
    private static SignalBatch Batch(params string[] names)
        => new()
        {
            Signals = names.Select(n => new TelemetrySignal
            {
                Category = SignalCategory.Usage,
                Name = n,
                Severity = SignalSeverity.Information
            }).ToList(),
            CreatedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task ExportAsync_AddsSignals()
    {
        var sink = new InMemoryTelemetrySink();
        await sink.ExportAsync(Batch("a", "b"));
        Assert.Equal(2, sink.Count);
    }

    [Fact]
    public async Task GetSignals_ReturnsSnapshot()
    {
        var sink = new InMemoryTelemetrySink();
        await sink.ExportAsync(Batch("x"));
        var snapshot = sink.GetSignals();
        await sink.ExportAsync(Batch("y"));

        Assert.Single(snapshot);
        Assert.Equal(2, sink.Count);
    }

    [Fact]
    public async Task GetSignals_OrderIsOldestFirst()
    {
        var sink = new InMemoryTelemetrySink();
        await sink.ExportAsync(Batch("first", "second"));
        var signals = sink.GetSignals();
        Assert.Equal("first", signals[0].Name);
        Assert.Equal("second", signals[1].Name);
    }

    [Fact]
    public async Task Clear_EmptiesSink()
    {
        var sink = new InMemoryTelemetrySink();
        await sink.ExportAsync(Batch("a", "b"));
        sink.Clear();
        Assert.Equal(0, sink.Count);
        Assert.Empty(sink.GetSignals());
    }

    [Fact]
    public async Task ExportAsync_ReturnsSuccess()
    {
        var sink = new InMemoryTelemetrySink();
        var result = await sink.ExportAsync(Batch("a"));
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Capacity_DropsOldestWhenExceeded()
    {
        var sink = new InMemoryTelemetrySink(capacity: 3);
        await sink.ExportAsync(Batch("a", "b", "c", "d"));
        var signals = sink.GetSignals();
        Assert.Equal(3, signals.Count);
        Assert.Equal("b", signals[0].Name);
        Assert.Equal("c", signals[1].Name);
        Assert.Equal("d", signals[2].Name);
    }

    [Fact]
    public async Task Capacity_MultipleExports_EvictsOldest()
    {
        var sink = new InMemoryTelemetrySink(capacity: 2);
        await sink.ExportAsync(Batch("a", "b"));
        await sink.ExportAsync(Batch("c"));
        var signals = sink.GetSignals();
        Assert.Equal(2, signals.Count);
        Assert.Equal("b", signals[0].Name);
        Assert.Equal("c", signals[1].Name);
    }

    [Fact]
    public void Constructor_ZeroCapacity_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryTelemetrySink(0));

    [Fact]
    public void Constructor_NegativeCapacity_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryTelemetrySink(-1));

    [Fact]
    public async Task ExportAsync_NullBatch_Throws()
    {
        var sink = new InMemoryTelemetrySink();
        await Assert.ThrowsAsync<ArgumentNullException>(() => sink.ExportAsync(null!).AsTask());
    }

    [Fact]
    public async Task EmptyBatch_DoesNotChangeCount()
    {
        var sink = new InMemoryTelemetrySink();
        await sink.ExportAsync(new SignalBatch { Signals = [], CreatedAt = DateTimeOffset.UtcNow });
        Assert.Equal(0, sink.Count);
    }
}
