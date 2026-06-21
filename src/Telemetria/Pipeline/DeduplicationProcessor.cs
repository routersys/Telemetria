using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Telemetria.Pipeline;

/// <summary>
/// 設定されたウィンドウ内で同一の名称とカテゴリを持つ重複信号を抑制します。
/// </summary>
public sealed class DeduplicationProcessor : ISignalProcessor
{
    private readonly IOptionsMonitor<DeduplicationOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new(StringComparer.Ordinal);

    /// <summary>依存関係を指定して初期化します。</summary>
    public DeduplicationProcessor(IOptionsMonitor<DeduplicationOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public TelemetrySignal? Process(TelemetrySignal signal)
    {
        var opts = _options.CurrentValue;

        if (opts.PassThroughErrors && signal.Category is SignalCategory.Error or SignalCategory.Exception)
        {
            return signal;
        }

        var key = $"{(int)signal.Category}:{signal.Name}";
        var now = _timeProvider.GetUtcNow();

        if (_seen.TryGetValue(key, out var lastSeen) && now - lastSeen < opts.Window)
        {
            return null;
        }

        _seen[key] = now;
        PruneExpired(now, opts.Window);
        return signal;
    }

    private void PruneExpired(DateTimeOffset now, TimeSpan window)
    {
        foreach (var kvp in _seen)
        {
            if (now - kvp.Value >= window)
            {
                _seen.TryRemove(kvp.Key, out _);
            }
        }
    }
}
