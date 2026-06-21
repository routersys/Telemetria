namespace Telemetria.Sinks;

/// <summary>
/// 何も行わないシンクです。送出を無効化する場合に用います。
/// </summary>
public sealed class NullTelemetrySink : ITelemetrySink
{
    /// <summary>共有インスタンスです。</summary>
    public static NullTelemetrySink Instance { get; } = new();

    private NullTelemetrySink()
    {
    }

    /// <inheritdoc />
    public ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(SinkResult.Success);
}
