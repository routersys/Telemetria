using Microsoft.Extensions.Options;

namespace Telemetria.Pipeline;

/// <summary>
/// 設定された最小重大度に満たない信号を除外します。
/// </summary>
public sealed class SeverityFilterProcessor : ISignalProcessor
{
    private readonly IOptionsMonitor<TelemetriaOptions> _options;

    /// <summary>オプション監視で初期化します。</summary>
    public SeverityFilterProcessor(IOptionsMonitor<TelemetriaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public TelemetrySignal? Process(TelemetrySignal signal)
        => signal.Severity >= _options.CurrentValue.MinimumSeverity ? signal : null;
}
