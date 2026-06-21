using Microsoft.Extensions.Logging;

namespace Telemetria.Sinks;

/// <summary>
/// バッチを暗号化エンベロープへ変換し、通信手段を介して送信するシンクです。
/// </summary>
public sealed class TransportTelemetrySink : ITelemetrySink
{
    private readonly IEnvelopeFactory _envelopeFactory;
    private readonly ITelemetryTransport _transport;
    private readonly ILogger<TransportTelemetrySink> _logger;

    /// <summary>依存関係を指定して初期化します。</summary>
    public TransportTelemetrySink(
        IEnvelopeFactory envelopeFactory,
        ITelemetryTransport transport,
        ILogger<TransportTelemetrySink> logger)
    {
        ArgumentNullException.ThrowIfNull(envelopeFactory);
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(logger);

        _envelopeFactory = envelopeFactory;
        _transport = transport;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        try
        {
            var envelope = _envelopeFactory.Create(batch);
            var accepted = await _transport.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
            return accepted ? SinkResult.Success : SinkResult.Failure("transport-rejected");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "テレメトリの送信に失敗しました。");
            return SinkResult.Failure(ex.Message);
        }
    }
}
