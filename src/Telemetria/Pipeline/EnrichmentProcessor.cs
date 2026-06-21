namespace Telemetria.Pipeline;

/// <summary>
/// 登録されたすべての <see cref="ISignalEnricher"/> を適用します。
/// </summary>
public sealed class EnrichmentProcessor : ISignalProcessor
{
    private readonly IReadOnlyList<ISignalEnricher> _enrichers;

    /// <summary>付与処理の一覧で初期化します。</summary>
    public EnrichmentProcessor(IEnumerable<ISignalEnricher> enrichers)
    {
        ArgumentNullException.ThrowIfNull(enrichers);
        _enrichers = [.. enrichers];
    }

    /// <inheritdoc />
    public TelemetrySignal? Process(TelemetrySignal signal)
    {
        var current = signal;
        foreach (var enricher in _enrichers)
        {
            current = enricher.Enrich(current);
        }

        return current;
    }
}
