using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Telemetria.Buffering;
using Telemetria.Collection;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Collection;

public sealed class TelemetryScopeTests
{
    private sealed class NoopExporter : ISignalExporter
    {
        public ValueTask<int> PumpAsync(CancellationToken cancellationToken) => ValueTask.FromResult(0);
        public ValueTask FlushAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private static (TelemetryClient Client, ChannelSignalBuffer Buffer, FakeTimeProvider Clock) Create()
    {
        var buffer = new ChannelSignalBuffer(Options.Create(new BufferOptions { Capacity = 16 }));
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(1000));
        var monitor = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions());
        var client = new TelemetryClient(buffer, new NoopExporter(), clock, monitor, NullLogger<TelemetryClient>.Instance);
        return (client, buffer, clock);
    }

    [Fact]
    public void Dispose_RecordsDiagnosticSignal()
    {
        var (client, buffer, _) = Create();
        using (client.BeginOperation("my-op")) { }

        Assert.True(buffer.TryReadBatch(10, out var batch));
        var signal = Assert.Single(batch);
        Assert.Equal(SignalCategory.Diagnostic, signal.Category);
        Assert.Equal("my-op", signal.Name);
        Assert.Equal(SignalSeverity.Information, signal.Severity);
    }

    [Fact]
    public async Task DisposeAsync_RecordsDiagnosticSignal()
    {
        var (client, buffer, _) = Create();
        await using (client.BeginOperation("async-op")) { }

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal("async-op", Assert.Single(batch).Name);
    }

    [Fact]
    public void Dispose_RecordsDurationMeasurement()
    {
        var (client, buffer, clock) = Create();
        var scope = client.BeginOperation("timed-op");
        clock.Advance(TimeSpan.FromMilliseconds(250));
        scope.Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal(250.0, Assert.Single(batch).Measurements["duration_ms"]);
    }

    [Fact]
    public void Timestamp_IsOperationStartTime()
    {
        var (client, buffer, clock) = Create();
        var startTime = clock.GetUtcNow();
        var scope = client.BeginOperation("op");
        clock.Advance(TimeSpan.FromSeconds(1));
        scope.Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal(startTime, Assert.Single(batch).Timestamp);
    }

    [Fact]
    public void Fail_RecordsExceptionSignal()
    {
        var (client, buffer, _) = Create();
        using (var scope = client.BeginOperation("failing-op"))
        {
            scope.Fail(new InvalidOperationException("boom"));
        }

        Assert.True(buffer.TryReadBatch(10, out var batch));
        var signal = Assert.Single(batch);
        Assert.Equal(SignalCategory.Exception, signal.Category);
        Assert.Equal("failing-op", signal.Name);
        Assert.Equal(SignalSeverity.Error, signal.Severity);
        Assert.NotNull(signal.Exception);
        Assert.Equal("boom", signal.Exception.Message);
    }

    [Fact]
    public void Fail_RecordsDurationMeasurement()
    {
        var (client, buffer, clock) = Create();
        var scope = client.BeginOperation("failing-op");
        clock.Advance(TimeSpan.FromMilliseconds(100));
        scope.Fail(new Exception("err"));
        scope.Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.True(Assert.Single(batch).Measurements.ContainsKey("duration_ms"));
    }

    [Fact]
    public void AddProperty_AppearsInSignal()
    {
        var (client, buffer, _) = Create();
        client.BeginOperation("op").AddProperty("key", "val").Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal("val", Assert.Single(batch).Properties["key"]);
    }

    [Fact]
    public void AddMeasurement_AppearsInSignal()
    {
        var (client, buffer, _) = Create();
        client.BeginOperation("op").AddMeasurement("count", 42.0).Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal(42.0, Assert.Single(batch).Measurements["count"]);
    }

    [Fact]
    public void InitialProperties_AppearsInSignal()
    {
        var (client, buffer, _) = Create();
        client.BeginOperation("op", new Dictionary<string, string> { ["src"] = "test" }).Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal("test", Assert.Single(batch).Properties["src"]);
    }

    [Fact]
    public void AddProperty_FluentChainingWorks()
    {
        var (client, buffer, _) = Create();
        client.BeginOperation("op")
            .AddProperty("a", "1")
            .AddProperty("b", "2")
            .Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        var signal = Assert.Single(batch);
        Assert.Equal("1", signal.Properties["a"]);
        Assert.Equal("2", signal.Properties["b"]);
    }

    [Fact]
    public void Dispose_Twice_RecordsOnce()
    {
        var (client, buffer, _) = Create();
        var scope = client.BeginOperation("op");
        scope.Dispose();
        scope.Dispose();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Single(batch);
    }

    [Fact]
    public async Task DisposeAsync_AfterDispose_IsIdempotent()
    {
        var (client, buffer, _) = Create();
        var scope = client.BeginOperation("op");
        scope.Dispose();
        await scope.DisposeAsync();

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Single(batch);
    }

    [Fact]
    public void BeginOperation_NullName_Throws()
    {
        var (client, _, _) = Create();
        Assert.Throws<ArgumentNullException>(() => client.BeginOperation(null!));
    }

    [Fact]
    public void BeginOperation_EmptyName_Throws()
    {
        var (client, _, _) = Create();
        Assert.Throws<ArgumentException>(() => client.BeginOperation(string.Empty));
    }

    [Fact]
    public void AddProperty_NullKey_Throws()
    {
        var (client, _, _) = Create();
        using var scope = client.BeginOperation("op");
        Assert.Throws<ArgumentNullException>(() => scope.AddProperty(null!, "v"));
    }

    [Fact]
    public void AddMeasurement_NullKey_Throws()
    {
        var (client, _, _) = Create();
        using var scope = client.BeginOperation("op");
        Assert.Throws<ArgumentNullException>(() => scope.AddMeasurement(null!, 0.0));
    }

    [Fact]
    public void Fail_NullException_Throws()
    {
        var (client, _, _) = Create();
        using var scope = client.BeginOperation("op");
        Assert.Throws<ArgumentNullException>(() => scope.Fail(null!));
    }

    [Fact]
    public void WhenDisabled_DisposeDoesNothing()
    {
        var buffer = new ChannelSignalBuffer(Options.Create(new BufferOptions { Capacity = 16 }));
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var monitor = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions { Mode = TelemetriaMode.Disabled });
        var client = new TelemetryClient(buffer, new NoopExporter(), clock, monitor, NullLogger<TelemetryClient>.Instance);

        using (client.BeginOperation("op")) { }

        Assert.Equal(0, buffer.Count);
    }
}
