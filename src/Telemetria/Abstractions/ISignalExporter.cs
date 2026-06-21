namespace Telemetria;

/// <summary>
/// バッファから信号を取り出し、パイプラインを適用してシンクへ送出する処理を表します。
/// </summary>
public interface ISignalExporter
{
    /// <summary>一回分のバッチを取り出して送出します。送出した信号数を返します。</summary>
    ValueTask<int> PumpAsync(CancellationToken cancellationToken);

    /// <summary>現在蓄積されているすべての信号を送出します。</summary>
    ValueTask FlushAsync(CancellationToken cancellationToken);
}
