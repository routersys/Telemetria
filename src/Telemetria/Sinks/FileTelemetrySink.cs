using System.Text;
using Microsoft.Extensions.Options;

namespace Telemetria.Sinks;

/// <summary>
/// サーバーを用いずにバッチをローカルファイルへ追記するシンクです。各行が一つのバッチに対応します。
/// </summary>
public sealed class FileTelemetrySink : ITelemetrySink
{
    private readonly ISignalSerializer _serializer;
    private readonly IOptionsMonitor<FileSinkOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>依存関係を指定して初期化します。</summary>
    public FileTelemetrySink(
        ISignalSerializer serializer,
        IOptionsMonitor<FileSinkOptions> options,
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
    public async ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var options = _options.CurrentValue;
        Directory.CreateDirectory(options.Directory);

        var fileName = $"{options.FileNamePrefix}-{_timeProvider.GetUtcNow():yyyyMMdd}.ndjson";
        var path = Path.Combine(options.Directory, fileName);
        var line = Encoding.UTF8.GetString(_serializer.Serialize(batch));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
            return SinkResult.Success;
        }
        finally
        {
            _gate.Release();
        }
    }
}
