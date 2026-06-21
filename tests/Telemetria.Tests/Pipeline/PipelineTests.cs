using Telemetria.Pipeline;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Pipeline;

public sealed class PipelineTests
{
    private static TelemetrySignal Signal(SignalCategory category = SignalCategory.Usage, SignalSeverity severity = SignalSeverity.Information)
        => new() { Category = category, Name = "s", Severity = severity };

    private sealed class DropProcessor : ISignalProcessor
    {
        public TelemetrySignal? Process(TelemetrySignal signal) => null;
    }

    private sealed class RenameProcessor : ISignalProcessor
    {
        private readonly string _name;

        public RenameProcessor(string name) => _name = name;

        public TelemetrySignal? Process(TelemetrySignal signal) => signal with { Name = _name };
    }

    [Fact]
    public void Run_AppliesProcessorsInOrder()
    {
        var pipeline = new SignalProcessorPipeline([new RenameProcessor("a"), new RenameProcessor("b")]);
        var result = pipeline.Run(Signal());
        Assert.Equal("b", result!.Name);
    }

    [Fact]
    public void Run_StopsAndReturnsNull_WhenProcessorDrops()
    {
        var pipeline = new SignalProcessorPipeline([new RenameProcessor("a"), new DropProcessor(), new RenameProcessor("b")]);
        Assert.Null(pipeline.Run(Signal()));
    }

    [Fact]
    public void Run_NoProcessors_ReturnsSignalUnchanged()
    {
        var pipeline = new SignalProcessorPipeline([]);
        var signal = Signal();
        Assert.Same(signal, pipeline.Run(signal));
    }

    [Fact]
    public void SeverityFilter_DropsBelowMinimum()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions { MinimumSeverity = SignalSeverity.Warning });
        var processor = new SeverityFilterProcessor(options);

        Assert.Null(processor.Process(Signal(severity: SignalSeverity.Information)));
        Assert.NotNull(processor.Process(Signal(severity: SignalSeverity.Error)));
    }

    [Fact]
    public void Sampling_RateOne_KeepsAll()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions { SamplingRate = 1.0 });
        var processor = new SamplingProcessor(options, () => 0.99);
        Assert.NotNull(processor.Process(Signal()));
    }

    [Fact]
    public void Sampling_RateZero_DropsUsage()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions { SamplingRate = 0.0 });
        var processor = new SamplingProcessor(options, () => 0.0);
        Assert.Null(processor.Process(Signal()));
    }

    [Fact]
    public void Sampling_AlwaysKeepsErrorsAndExceptions()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions { SamplingRate = 0.0 });
        var processor = new SamplingProcessor(options, () => 0.99);

        Assert.NotNull(processor.Process(Signal(SignalCategory.Error)));
        Assert.NotNull(processor.Process(Signal(SignalCategory.Exception)));
    }

    [Fact]
    public void Sampling_UsesSamplerThreshold()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions { SamplingRate = 0.5 });

        var keep = new SamplingProcessor(options, () => 0.4);
        var drop = new SamplingProcessor(options, () => 0.6);

        Assert.NotNull(keep.Process(Signal()));
        Assert.Null(drop.Process(Signal()));
    }

    [Fact]
    public void Enrichment_AppliesAllEnrichers()
    {
        var enricher = new DelegateEnricher(s => s with { Name = s.Name + "!" });
        var processor = new EnrichmentProcessor([enricher, enricher]);
        Assert.Equal("s!!", processor.Process(Signal())!.Name);
    }

    private sealed class DelegateEnricher : ISignalEnricher
    {
        private readonly Func<TelemetrySignal, TelemetrySignal> _enrich;

        public DelegateEnricher(Func<TelemetrySignal, TelemetrySignal> enrich) => _enrich = enrich;

        public TelemetrySignal Enrich(TelemetrySignal signal) => _enrich(signal);
    }
}
