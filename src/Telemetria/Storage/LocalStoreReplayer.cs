using Microsoft.Extensions.Logging;

namespace Telemetria.Storage;

/// <summary>
/// ローカルへ退避されたバッチを再送し、成功した項目を削除します。
/// </summary>
public sealed class LocalStoreReplayer
{
    private readonly ILocalSignalStore _store;
    private readonly IEnvelopeFactory _envelopeFactory;
    private readonly ITelemetryTransport _transport;
    private readonly ILogger<LocalStoreReplayer> _logger;

    /// <summary>依存関係を指定して初期化します。</summary>
    public LocalStoreReplayer(
        ILocalSignalStore store,
        IEnvelopeFactory envelopeFactory,
        ITelemetryTransport transport,
        ILogger<LocalStoreReplayer> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(envelopeFactory);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _envelopeFactory = envelopeFactory;
        _transport = transport;
        _logger = logger;
    }

    /// <summary>退避済みのバッチを順に再送します。再送に成功した件数を返します。</summary>
    public async ValueTask<int> ReplayAsync(CancellationToken cancellationToken)
    {
        var replayed = 0;
        await foreach (var stored in _store.ReadPendingAsync(cancellationToken).ConfigureAwait(false))
        {
            bool accepted;
            try
            {
                var envelope = _envelopeFactory.Create(stored.Batch);
                accepted = await _transport.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "退避済みバッチの再送に失敗しました。");
                break;
            }

            if (!accepted)
            {
                break;
            }

            await _store.RemoveAsync(stored.Token, cancellationToken).ConfigureAwait(false);
            replayed++;
        }

        return replayed;
    }
}
