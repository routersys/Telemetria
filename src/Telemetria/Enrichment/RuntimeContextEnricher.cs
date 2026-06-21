using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Telemetria.Enrichment;

/// <summary>
/// 実行環境の文脈情報を信号へ付与します。個人を特定する情報は含めません。
/// </summary>
public sealed class RuntimeContextEnricher : ISignalEnricher
{
    private readonly IOptionsMonitor<TelemetriaOptions> _options;

    /// <summary>オプション監視で初期化します。</summary>
    public RuntimeContextEnricher(IOptionsMonitor<TelemetriaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public TelemetrySignal Enrich(TelemetrySignal signal)
    {
        var options = _options.CurrentValue;
        var properties = new Dictionary<string, string>(signal.Properties, StringComparer.Ordinal)
        {
            ["app.name"] = options.ApplicationName,
            ["app.version"] = options.ApplicationVersion,
            ["runtime.framework"] = RuntimeInformation.FrameworkDescription,
            ["runtime.os"] = RuntimeInformation.OSDescription,
            ["runtime.arch"] = RuntimeInformation.ProcessArchitecture.ToString()
        };

        return signal with { Properties = properties };
    }
}
