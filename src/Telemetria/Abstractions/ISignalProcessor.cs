namespace Telemetria;

/// <summary>
/// パイプラインの一段として信号を変換または除外する処理を表します。
/// </summary>
public interface ISignalProcessor
{
    /// <summary>信号を処理します。除外する場合は <see langword="null"/> を返します。</summary>
    TelemetrySignal? Process(TelemetrySignal signal);
}
