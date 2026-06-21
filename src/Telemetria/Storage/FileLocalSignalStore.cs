using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Telemetria.Storage;

/// <summary>
/// 送信に失敗したバッチをファイルとしてローカルに保管します。
/// </summary>
public sealed class FileLocalSignalStore : ILocalSignalStore
{
    private const string Extension = ".batch";

    private readonly ISignalSerializer _serializer;
    private readonly IOptionsMonitor<LocalStoreOptions> _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>依存関係を指定して初期化します。</summary>
    public FileLocalSignalStore(
        ISignalSerializer serializer,
        IOptionsMonitor<LocalStoreOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _serializer = serializer;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask StoreAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var options = _options.CurrentValue;
        var directory = options.Directory;
        Directory.CreateDirectory(directory);

        if (CountItems(directory) >= options.MaxItems)
        {
            RemoveOldest(directory);
        }

        var token = $"{_timeProvider.GetUtcNow():yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{Extension}";
        var path = Path.Combine(directory, token);
        await File.WriteAllBytesAsync(path, _serializer.Serialize(batch), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StoredBatch> ReadPendingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var directory = _options.CurrentValue.Directory;
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*" + Extension).Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            SignalBatch batch;
            try
            {
                var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                batch = _serializer.Deserialize(bytes);
            }
            catch (Exception ex) when (ex is IOException or FormatException)
            {
                continue;
            }

            yield return new StoredBatch
            {
                Token = Path.GetFileName(path),
                Batch = batch
            };
        }
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        var path = Path.Combine(_options.CurrentValue.Directory, token);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return ValueTask.CompletedTask;
    }

    private static int CountItems(string directory)
        => Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*" + Extension).Count() : 0;

    private static void RemoveOldest(string directory)
    {
        var oldest = Directory.EnumerateFiles(directory, "*" + Extension).Order(StringComparer.Ordinal).FirstOrDefault();
        if (oldest is not null)
        {
            File.Delete(oldest);
        }
    }
}
