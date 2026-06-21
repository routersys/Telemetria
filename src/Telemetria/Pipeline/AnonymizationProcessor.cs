namespace Telemetria.Pipeline;

/// <summary>
/// 信号に匿名化処理を適用します。
/// </summary>
public sealed class AnonymizationProcessor : ISignalProcessor
{
    private readonly IAnonymizer _anonymizer;

    /// <summary>匿名化処理で初期化します。</summary>
    public AnonymizationProcessor(IAnonymizer anonymizer)
    {
        ArgumentNullException.ThrowIfNull(anonymizer);
        _anonymizer = anonymizer;
    }

    /// <inheritdoc />
    public TelemetrySignal? Process(TelemetrySignal signal) => _anonymizer.Anonymize(signal);
}
