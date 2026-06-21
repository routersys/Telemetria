using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telemetria.Storage;

namespace Telemetria.Hosting;

/// <summary>
/// 背景でバッファを送出し、退避済みバッチを定期的に再送するサービスです。
/// </summary>
public sealed class TelemetryDispatcher : BackgroundService
{
    private readonly ISignalExporter _exporter;
    private readonly LocalStoreReplayer? _replayer;
    private readonly IOptionsMonitor<DispatchOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TelemetryDispatcher> _logger;

    /// <summary>依存関係を指定して初期化します。</summary>
    public TelemetryDispatcher(
        ISignalExporter exporter,
        LocalStoreReplayer? replayer,
        IOptionsMonitor<DispatchOptions> options,
        TimeProvider timeProvider,
        ILogger<TelemetryDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _exporter = exporter;
        _replayer = replayer;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastReplay = _timeProvider.GetUtcNow();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _exporter.PumpAsync(stoppingToken).ConfigureAwait(false);

                if (_replayer is not null)
                {
                    var now = _timeProvider.GetUtcNow();
                    if (now - lastReplay >= _options.CurrentValue.ReplayInterval)
                    {
                        await _replayer.ReplayAsync(stoppingToken).ConfigureAwait(false);
                        lastReplay = now;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "送出ループで予期しない例外が発生しました。");
            }
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _exporter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "停止時のフラッシュに失敗しました。");
        }
    }
}
