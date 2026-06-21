namespace Telemetria.Sinks;

/// <summary>
/// 複数のシンクへ同一のバッチを送出します。すべて成功した場合のみ成功とみなします。
/// </summary>
public sealed class CompositeTelemetrySink : ITelemetrySink
{
    private readonly IReadOnlyList<ITelemetrySink> _sinks;

    /// <summary>送出先シンクの一覧で初期化します。</summary>
    public CompositeTelemetrySink(IEnumerable<ITelemetrySink> sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        _sinks = [.. sinks];
    }

    /// <inheritdoc />
    public async ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var succeeded = true;
        string? detail = null;
        foreach (var sink in _sinks)
        {
            var result = await sink.ExportAsync(batch, cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                succeeded = false;
                detail = result.Detail;
            }
        }

        return succeeded ? SinkResult.Success : SinkResult.Failure(detail ?? "composite-failure");
    }
}
