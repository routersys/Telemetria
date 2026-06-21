namespace Telemetria;

/// <summary>
/// 信号に追加の文脈情報を付与する処理を表します。
/// </summary>
public interface ISignalEnricher
{
    /// <summary>信号に情報を付与します。</summary>
    TelemetrySignal Enrich(TelemetrySignal signal);
}
