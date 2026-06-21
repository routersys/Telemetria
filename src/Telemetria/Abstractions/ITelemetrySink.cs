namespace Telemetria;

/// <summary>
/// バッチの最終的な送出先を表します。送信先サーバーやローカルファイルなどを抽象化します。
/// </summary>
public interface ITelemetrySink
{
    /// <summary>バッチを送出します。</summary>
    ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default);
}
