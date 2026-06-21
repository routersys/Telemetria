using System.Collections.Concurrent;

namespace Telemetria.Sinks;

/// <summary>
/// 最近のテレメトリ信号をメモリ上に保持するシンクです。診断・開発環境での利用を想定します。
/// </summary>
public sealed class InMemoryTelemetrySink : ITelemetrySink
{
    private readonly int _capacity;
    private readonly ConcurrentQueue<TelemetrySignal> _signals = new();

    /// <summary>保持する最大信号数を指定して初期化します。</summary>
    public InMemoryTelemetrySink(int capacity = 1000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);
        _capacity = capacity;
    }

    /// <summary>現在保持している信号の数です。</summary>
    public int Count => _signals.Count;

    /// <summary>保持しているすべての信号のスナップショットを返します。古い順に並んでいます。</summary>
    public IReadOnlyList<TelemetrySignal> GetSignals() => _signals.ToArray();

    /// <summary>保持しているすべての信号を破棄します。</summary>
    public void Clear()
    {
        while (_signals.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        foreach (var signal in batch.Signals)
        {
            _signals.Enqueue(signal);
        }

        while (_signals.Count > _capacity)
        {
            _signals.TryDequeue(out _);
        }

        return ValueTask.FromResult(SinkResult.Success);
    }
}
