using Telemetria.Enrichment;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Enrichment;

public sealed class RuntimeContextEnricherTests
{
    [Fact]
    public void Enrich_AddsApplicationAndRuntimeContext()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions
        {
            ApplicationName = "MyApp",
            ApplicationVersion = "9.9.9"
        });
        var enricher = new RuntimeContextEnricher(options);

        var result = enricher.Enrich(new TelemetrySignal { Category = SignalCategory.Usage, Name = "s" });

        Assert.Equal("MyApp", result.Properties["app.name"]);
        Assert.Equal("9.9.9", result.Properties["app.version"]);
        Assert.True(result.Properties.ContainsKey("runtime.framework"));
        Assert.True(result.Properties.ContainsKey("runtime.os"));
        Assert.True(result.Properties.ContainsKey("runtime.arch"));
    }

    [Fact]
    public void Enrich_PreservesExistingProperties()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions());
        var enricher = new RuntimeContextEnricher(options);

        var result = enricher.Enrich(new TelemetrySignal
        {
            Category = SignalCategory.Usage,
            Name = "s",
            Properties = new Dictionary<string, string> { ["custom"] = "value" }
        });

        Assert.Equal("value", result.Properties["custom"]);
    }

    [Fact]
    public void Enrich_DoesNotLeakMachineName()
    {
        var options = new StaticOptionsMonitor<TelemetriaOptions>(new TelemetriaOptions());
        var enricher = new RuntimeContextEnricher(options);

        var result = enricher.Enrich(new TelemetrySignal { Category = SignalCategory.Usage, Name = "s" });

        Assert.DoesNotContain(result.Properties.Values, v => v.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase));
    }
}
