using Microsoft.Extensions.Time.Testing;
using Telemetria.Pipeline;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Pipeline;

public sealed class DeduplicationProcessorTests
{
    private static TelemetrySignal Signal(string name = "evt", SignalCategory category = SignalCategory.Usage)
        => new() { Category = category, Name = name, Severity = SignalSeverity.Information };

    private static (DeduplicationProcessor Processor, FakeTimeProvider Clock, StaticOptionsMonitor<DeduplicationOptions> Options) Create(
        TimeSpan? window = null,
        bool passThroughErrors = true)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddSeconds(1000));
        var opts = new StaticOptionsMonitor<DeduplicationOptions>(new DeduplicationOptions
        {
            Window = window ?? TimeSpan.FromMinutes(1),
            PassThroughErrors = passThroughErrors
        });
        return (new DeduplicationProcessor(opts, clock), clock, opts);
    }

    [Fact]
    public void FirstSignal_Passes()
    {
        var (proc, _, _) = Create();
        Assert.NotNull(proc.Process(Signal()));
    }

    [Fact]
    public void SecondIdenticalSignal_WithinWindow_IsDropped()
    {
        var (proc, _, _) = Create();
        proc.Process(Signal());
        Assert.Null(proc.Process(Signal()));
    }

    [Fact]
    public void SignalAfterWindowExpiry_Passes()
    {
        var (proc, clock, _) = Create(window: TimeSpan.FromMinutes(1));
        proc.Process(Signal());
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.NotNull(proc.Process(Signal()));
    }

    [Fact]
    public void SignalJustBeforeWindowExpiry_IsDropped()
    {
        var (proc, clock, _) = Create(window: TimeSpan.FromMinutes(1));
        proc.Process(Signal());
        clock.Advance(TimeSpan.FromSeconds(59));
        Assert.Null(proc.Process(Signal()));
    }

    [Fact]
    public void DifferentName_Passes()
    {
        var (proc, _, _) = Create();
        proc.Process(Signal("a"));
        Assert.NotNull(proc.Process(Signal("b")));
    }

    [Fact]
    public void SameNameDifferentCategory_Passes()
    {
        var (proc, _, _) = Create();
        proc.Process(Signal("evt", SignalCategory.Usage));
        Assert.NotNull(proc.Process(Signal("evt", SignalCategory.Diagnostic)));
    }

    [Fact]
    public void ErrorCategory_PassesThroughWhenEnabled()
    {
        var (proc, _, _) = Create(passThroughErrors: true);
        proc.Process(Signal("err", SignalCategory.Error));
        Assert.NotNull(proc.Process(Signal("err", SignalCategory.Error)));
    }

    [Fact]
    public void ExceptionCategory_PassesThroughWhenEnabled()
    {
        var (proc, _, _) = Create(passThroughErrors: true);
        proc.Process(Signal("ex", SignalCategory.Exception));
        Assert.NotNull(proc.Process(Signal("ex", SignalCategory.Exception)));
    }

    [Fact]
    public void ErrorCategory_IsDeduplicatedWhenDisabled()
    {
        var (proc, _, _) = Create(passThroughErrors: false);
        proc.Process(Signal("err", SignalCategory.Error));
        Assert.Null(proc.Process(Signal("err", SignalCategory.Error)));
    }

    [Fact]
    public void MultipleDistinctSignals_AllPass()
    {
        var (proc, _, _) = Create();
        for (var i = 0; i < 10; i++)
        {
            Assert.NotNull(proc.Process(Signal($"evt-{i}")));
        }
    }

    [Fact]
    public void WindowOptionChange_IsRespected()
    {
        var (proc, clock, opts) = Create(window: TimeSpan.FromMinutes(5));
        proc.Process(Signal());
        clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Null(proc.Process(Signal()));

        opts.Set(new DeduplicationOptions { Window = TimeSpan.FromMinutes(1) });
        Assert.NotNull(proc.Process(Signal()));
    }

    [Fact]
    public void NullOptions_Throws()
        => Assert.Throws<ArgumentNullException>(() => new DeduplicationProcessor(null!, TimeProvider.System));

    [Fact]
    public void NullTimeProvider_Throws()
        => Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationProcessor(new StaticOptionsMonitor<DeduplicationOptions>(new DeduplicationOptions()), null!));
}
