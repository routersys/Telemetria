namespace Telemetria;

/// <summary>
/// 送信に失敗したバッチをローカルに保管し、再送のために取り出す処理を表します。
/// </summary>
public interface ILocalSignalStore
{
    /// <summary>バッチを保管します。</summary>
    ValueTask StoreAsync(SignalBatch batch, CancellationToken cancellationToken = default);

    /// <summary>再送待ちのバッチを列挙します。</summary>
    IAsyncEnumerable<StoredBatch> ReadPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>指定したトークンの保管項目を削除します。</summary>
    ValueTask RemoveAsync(string token, CancellationToken cancellationToken = default);
}
