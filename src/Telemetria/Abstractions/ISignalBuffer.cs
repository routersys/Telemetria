namespace Telemetria;

/// <summary>
/// 信号を一時的に蓄積し、バッチ単位で取り出すためのバッファを表します。
/// </summary>
public interface ISignalBuffer
{
    /// <summary>信号を書き込みます。容量超過などで書き込めない場合は <see langword="false"/> を返します。</summary>
    bool TryWrite(TelemetrySignal signal);

    /// <summary>蓄積中の信号数を取得します。</summary>
    int Count { get; }

    /// <summary>最大件数または最大待機時間に達するまで信号を読み取りバッチを返します。</summary>
    ValueTask<IReadOnlyList<TelemetrySignal>> ReadBatchAsync(int maxBatchSize, TimeSpan maxWait, CancellationToken cancellationToken);

    /// <summary>待機せずに、現在蓄積されている信号をバッチとして取り出します。</summary>
    bool TryReadBatch(int maxBatchSize, out IReadOnlyList<TelemetrySignal> batch);

    /// <summary>これ以上の書き込みがないことを通知します。</summary>
    void Complete();
}
