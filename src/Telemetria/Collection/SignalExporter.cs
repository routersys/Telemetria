using Microsoft.Extensions.Options;

namespace Telemetria.Collection;

/// <summary>
/// バッファから信号を取り出し、パイプライン適用後にシンクへ送出します。
/// </summary>
public sealed class SignalExporter : ISignalExporter
{
    private readonly ISignalBuffer _buffer;
    private readonly ISignalPipeline _pipeline;
    private readonly ITelemetrySink _sink;
    private readonly IOptionsMonitor<DispatchOptions> _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>依存関係を指定して初期化します。</summary>
    public SignalExporter(
        ISignalBuffer buffer,
        ISignalPipeline pipeline,
        ITelemetrySink sink,
        IOptionsMonitor<DispatchOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _buffer = buffer;
        _pipeline = pipeline;
        _sink = sink;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<int> PumpAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        var signals = await _buffer.ReadBatchAsync(options.BatchSize, options.FlushInterval, cancellationToken).ConfigureAwait(false);
        return await ExportAsync(signals, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        var batchSize = _options.CurrentValue.BatchSize;
        while (_buffer.TryReadBatch(batchSize, out var signals))
        {
            await ExportAsync(signals, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<int> ExportAsync(IReadOnlyList<TelemetrySignal> signals, CancellationToken cancellationToken)
    {
        if (signals.Count == 0)
        {
            return 0;
        }

        var processed = new List<TelemetrySignal>(signals.Count);
        foreach (var signal in signals)
        {
            var result = _pipeline.Run(signal);
            if (result is not null)
            {
                processed.Add(result);
            }
        }

        if (processed.Count == 0)
        {
            return 0;
        }

        var batch = new SignalBatch
        {
            Signals = processed,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        await _sink.ExportAsync(batch, cancellationToken).ConfigureAwait(false);
        return processed.Count;
    }
}
