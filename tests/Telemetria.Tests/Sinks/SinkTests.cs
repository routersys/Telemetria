using Microsoft.Extensions.Logging.Abstractions;
using Telemetria.Sinks;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Sinks;

public sealed class SinkTests
{
    private static SignalBatch Batch()
        => new()
        {
            CreatedAt = DateTimeOffset.UnixEpoch,
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = "s" }]
        };

    [Fact]
    public async Task Failover_PrimarySucceeds_DoesNotStore()
    {
        var store = new InMemoryLocalStore();
        var sink = new FailoverTelemetrySink(new CapturingSink(SinkResult.Success), store, NullLogger<FailoverTelemetrySink>.Instance);

        var result = await sink.ExportAsync(Batch());

        Assert.True(result.Succeeded);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task Failover_PrimaryFails_StoresForRetry()
    {
        var store = new InMemoryLocalStore();
        var sink = new FailoverTelemetrySink(new CapturingSink(SinkResult.Failure("down")), store, NullLogger<FailoverTelemetrySink>.Instance);

        var result = await sink.ExportAsync(Batch());

        Assert.True(result.Succeeded);
        Assert.Equal("stored-for-retry", result.Detail);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task Composite_AllSucceed_ReturnsSuccess()
    {
        var a = new CapturingSink(SinkResult.Success);
        var b = new CapturingSink(SinkResult.Success);
        var sink = new CompositeTelemetrySink([a, b]);

        var result = await sink.ExportAsync(Batch());

        Assert.True(result.Succeeded);
        Assert.Equal(1, a.ExportCount);
        Assert.Equal(1, b.ExportCount);
    }

    [Fact]
    public async Task Composite_OneFails_ReturnsFailureButSendsToAll()
    {
        var a = new CapturingSink(SinkResult.Failure("x"));
        var b = new CapturingSink(SinkResult.Success);
        var sink = new CompositeTelemetrySink([a, b]);

        var result = await sink.ExportAsync(Batch());

        Assert.False(result.Succeeded);
        Assert.Equal(1, a.ExportCount);
        Assert.Equal(1, b.ExportCount);
    }

    [Fact]
    public async Task Null_AlwaysSucceeds()
    {
        var result = await NullTelemetrySink.Instance.ExportAsync(Batch());
        Assert.True(result.Succeeded);
    }
}
