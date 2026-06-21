using Microsoft.Extensions.Options;

namespace Telemetria.Pipeline;

/// <summary>
/// 設定された採取率に基づいて信号を確率的に除外します。例外およびエラーは常に採取します。
/// </summary>
public sealed class SamplingProcessor : ISignalProcessor
{
    private readonly IOptionsMonitor<TelemetriaOptions> _options;
    private readonly Func<double> _sampler;

    /// <summary>オプション監視と既定の乱数で初期化します。</summary>
    public SamplingProcessor(IOptionsMonitor<TelemetriaOptions> options)
        : this(options, static () => Random.Shared.NextDouble())
    {
    }

    /// <summary>オプション監視と任意の採取値生成器で初期化します。</summary>
    public SamplingProcessor(IOptionsMonitor<TelemetriaOptions> options, Func<double> sampler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sampler);
        _options = options;
        _sampler = sampler;
    }

    /// <inheritdoc />
    public TelemetrySignal? Process(TelemetrySignal signal)
    {
        if (signal.Category is SignalCategory.Exception or SignalCategory.Error)
        {
            return signal;
        }

        var rate = _options.CurrentValue.SamplingRate;
        if (rate >= 1.0)
        {
            return signal;
        }

        if (rate <= 0.0)
        {
            return null;
        }

        return _sampler() < rate ? signal : null;
    }
}
