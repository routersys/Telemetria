namespace Telemetria.Pipeline;

/// <summary>
/// 登録された <see cref="ISignalProcessor"/> を順に適用するパイプラインです。
/// </summary>
public sealed class SignalProcessorPipeline : ISignalPipeline
{
    private readonly IReadOnlyList<ISignalProcessor> _processors;

    /// <summary>適用順に並んだ処理の一覧で初期化します。</summary>
    public SignalProcessorPipeline(IEnumerable<ISignalProcessor> processors)
    {
        ArgumentNullException.ThrowIfNull(processors);
        _processors = [.. processors];
    }

    /// <inheritdoc />
    public TelemetrySignal? Run(TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        var current = signal;
        foreach (var processor in _processors)
        {
            var next = processor.Process(current);
            if (next is null)
            {
                return null;
            }

            current = next;
        }

        return current;
    }
}
