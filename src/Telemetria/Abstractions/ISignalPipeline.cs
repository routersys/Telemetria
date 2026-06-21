namespace Telemetria;

/// <summary>
/// 複数の <see cref="ISignalProcessor"/> を順に適用するパイプラインを表します。
/// </summary>
public interface ISignalPipeline
{
    /// <summary>信号にパイプラインを適用します。除外された場合は <see langword="null"/> を返します。</summary>
    TelemetrySignal? Run(TelemetrySignal signal);
}
