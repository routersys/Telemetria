using Microsoft.Extensions.Logging;

namespace Telemetria.Sinks;

/// <summary>
/// 主シンクへの送出に失敗した場合、ローカル退避ストアへ保管して後続の再送に委ねるシンクです。
/// </summary>
public sealed class FailoverTelemetrySink : ITelemetrySink
{
    private readonly ITelemetrySink _primary;
    private readonly ILocalSignalStore _store;
    private readonly ILogger<FailoverTelemetrySink> _logger;

    /// <summary>主シンクと退避ストアを指定して初期化します。</summary>
    public FailoverTelemetrySink(
        ITelemetrySink primary,
        ILocalSignalStore store,
        ILogger<FailoverTelemetrySink> logger)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _primary = primary;
        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var result = await _primary.ExportAsync(batch, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return result;
        }

        await _store.StoreAsync(batch, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("送信に失敗したバッチをローカルへ退避しました。件数: {Count}", batch.Signals.Count);
        return SinkResult.Deferred("stored-for-retry");
    }
}
