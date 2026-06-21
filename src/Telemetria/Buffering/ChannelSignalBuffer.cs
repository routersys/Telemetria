using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Telemetria.Buffering;

/// <summary>
/// <see cref="Channel{T}"/> を基盤とする有界の信号バッファです。
/// </summary>
public sealed class ChannelSignalBuffer : ISignalBuffer
{
    private readonly Channel<TelemetrySignal> _channel;

    /// <summary>オプションで初期化します。</summary>
    public ChannelSignalBuffer(IOptions<BufferOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value;
        var capacity = value.Capacity > 0 ? value.Capacity : 1;

        _channel = Channel.CreateBounded<TelemetrySignal>(new BoundedChannelOptions(capacity)
        {
            FullMode = value.FullMode == BufferFullMode.DropOldest
                ? BoundedChannelFullMode.DropOldest
                : BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    /// <inheritdoc />
    public int Count => _channel.Reader.Count;

    /// <inheritdoc />
    public bool TryWrite(TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        return _channel.Writer.TryWrite(signal);
    }

    /// <inheritdoc />
    public bool TryReadBatch(int maxBatchSize, out IReadOnlyList<TelemetrySignal> batch)
    {
        var list = new List<TelemetrySignal>(Math.Min(maxBatchSize, 64));
        while (list.Count < maxBatchSize && _channel.Reader.TryRead(out var signal))
        {
            list.Add(signal);
        }

        batch = list;
        return list.Count > 0;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<TelemetrySignal>> ReadBatchAsync(int maxBatchSize, TimeSpan maxWait, CancellationToken cancellationToken)
    {
        var batch = new List<TelemetrySignal>(Math.Min(maxBatchSize, 64));

        if (!await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return batch;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (maxWait > TimeSpan.Zero)
        {
            linked.CancelAfter(maxWait);
        }

        try
        {
            while (batch.Count < maxBatchSize)
            {
                if (_channel.Reader.TryRead(out var signal))
                {
                    batch.Add(signal);
                    continue;
                }

                if (batch.Count > 0 && maxWait <= TimeSpan.Zero)
                {
                    break;
                }

                if (!await _channel.Reader.WaitToReadAsync(linked.Token).ConfigureAwait(false))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
        }

        return batch;
    }

    /// <inheritdoc />
    public void Complete() => _channel.Writer.TryComplete();
}
