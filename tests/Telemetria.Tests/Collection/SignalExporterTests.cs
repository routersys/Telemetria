using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Telemetria.Buffering;
using Telemetria.Collection;
using Telemetria.Pipeline;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Collection;

public sealed class SignalExporterTests
{
    private sealed class DropExceptionsPipeline : ISignalPipeline
    {
        public TelemetrySignal? Run(TelemetrySignal signal)
            => signal.Category == SignalCategory.Exception ? null : signal;
    }

    private sealed class PassthroughPipeline : ISignalPipeline
    {
        public TelemetrySignal? Run(TelemetrySignal signal) => signal;
    }

    private static ChannelSignalBuffer Buffer(int capacity = 64)
        => new(Options.Create(new BufferOptions { Capacity = capacity }));

    private static TelemetrySignal Signal(SignalCategory category = SignalCategory.Usage)
        => new() { Category = category, Name = "s" };

    [Fact]
    public async Task FlushAsync_ExportsAllBufferedSignals()
    {
        var buffer = Buffer();
        for (var i = 0; i < 5; i++)
        {
            buffer.TryWrite(Signal());
        }

        var sink = new CapturingSink();
        var exporter = new SignalExporter(
            buffer,
            new PassthroughPipeline(),
            sink,
            new StaticOptionsMonitor<DispatchOptions>(new DispatchOptions { BatchSize = 2 }),
            new FakeTimeProvider());

        await exporter.FlushAsync(CancellationToken.None);

        var total = sink.Batches.Sum(b => b.Signals.Count);
        Assert.Equal(5, total);
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public async Task FlushAsync_DropsSignalsRemovedByPipeline()
    {
        var buffer = Buffer();
        buffer.TryWrite(Signal(SignalCategory.Exception));
        buffer.TryWrite(Signal(SignalCategory.Usage));

        var sink = new CapturingSink();
        var exporter = new SignalExporter(
            buffer,
            new DropExceptionsPipeline(),
            sink,
            new StaticOptionsMonitor<DispatchOptions>(new DispatchOptions { BatchSize = 10 }),
            new FakeTimeProvider());

        await exporter.FlushAsync(CancellationToken.None);

        var batch = Assert.Single(sink.Batches);
        Assert.Equal(SignalCategory.Usage, Assert.Single(batch.Signals).Category);
    }

    [Fact]
    public async Task FlushAsync_AllDropped_DoesNotExport()
    {
        var buffer = Buffer();
        buffer.TryWrite(Signal(SignalCategory.Exception));

        var sink = new CapturingSink();
        var exporter = new SignalExporter(
            buffer,
            new DropExceptionsPipeline(),
            sink,
            new StaticOptionsMonitor<DispatchOptions>(new DispatchOptions { BatchSize = 10 }),
            new FakeTimeProvider());

        await exporter.FlushAsync(CancellationToken.None);

        Assert.Equal(0, sink.ExportCount);
    }

    [Fact]
    public async Task FlushAsync_EmptyBuffer_DoesNotExport()
    {
        var sink = new CapturingSink();
        var exporter = new SignalExporter(
            Buffer(),
            new PassthroughPipeline(),
            sink,
            new StaticOptionsMonitor<DispatchOptions>(new DispatchOptions()),
            new FakeTimeProvider());

        await exporter.FlushAsync(CancellationToken.None);

        Assert.Equal(0, sink.ExportCount);
    }

    [Fact]
    public async Task PumpAsync_ExportsBatchAndReturnsCount()
    {
        var buffer = Buffer();
        buffer.TryWrite(Signal());
        buffer.TryWrite(Signal());

        var sink = new CapturingSink();
        var exporter = new SignalExporter(
            buffer,
            new PassthroughPipeline(),
            sink,
            new StaticOptionsMonitor<DispatchOptions>(new DispatchOptions { BatchSize = 10, FlushInterval = TimeSpan.FromMilliseconds(50) }),
            new FakeTimeProvider());

        var count = await exporter.PumpAsync(CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal(1, sink.ExportCount);
    }

    [Fact]
    public async Task ExportedBatch_HasTimestampFromClock()
    {
        var buffer = Buffer();
        buffer.TryWrite(Signal());

        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(777));
        var sink = new CapturingSink();
        var exporter = new SignalExporter(
            buffer,
            new PassthroughPipeline(),
            sink,
            new StaticOptionsMonitor<DispatchOptions>(new DispatchOptions { BatchSize = 10 }),
            clock);

        await exporter.FlushAsync(CancellationToken.None);

        Assert.True(sink.Batches.TryDequeue(out var batch));
        Assert.Equal(clock.GetUtcNow(), batch!.CreatedAt);
    }
}
