using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Telemetria.Collection;

/// <summary>
/// テレメトリ信号を記録する既定の実装です。
/// </summary>
public sealed class TelemetryClient : ITelemetryClient
{
    private readonly ISignalBuffer _buffer;
    private readonly ISignalExporter _exporter;
    private readonly TimeProvider _timeProvider;
    private readonly IOptionsMonitor<TelemetriaOptions> _options;
    private readonly ILogger<TelemetryClient> _logger;

    /// <summary>依存関係を指定して初期化します。</summary>
    public TelemetryClient(
        ISignalBuffer buffer,
        ISignalExporter exporter,
        TimeProvider timeProvider,
        IOptionsMonitor<TelemetriaOptions> options,
        ILogger<TelemetryClient> logger)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _buffer = buffer;
        _exporter = exporter;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Track(TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        if (_options.CurrentValue.Mode == TelemetriaMode.Disabled)
        {
            return;
        }

        var stamped = signal.Timestamp == default
            ? signal with { Timestamp = _timeProvider.GetUtcNow() }
            : signal;

        if (!_buffer.TryWrite(stamped))
        {
            _logger.LogDebug("バッファが満杯のため信号を破棄しました: {Name}", stamped.Name);
        }
    }

    /// <inheritdoc />
    public void TrackUsage(string name, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, double>? measurements = null)
        => Track(new TelemetrySignal
        {
            Category = SignalCategory.Usage,
            Name = Require(name),
            Severity = SignalSeverity.Information,
            Properties = Freeze(properties),
            Measurements = FreezeMeasurements(measurements)
        });

    /// <inheritdoc />
    public void TrackError(string name, SignalSeverity severity = SignalSeverity.Error, IReadOnlyDictionary<string, string>? properties = null)
        => Track(new TelemetrySignal
        {
            Category = SignalCategory.Error,
            Name = Require(name),
            Severity = severity,
            Properties = Freeze(properties)
        });

    /// <inheritdoc />
    public void TrackException(Exception exception, SignalSeverity severity = SignalSeverity.Error, IReadOnlyDictionary<string, string>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Track(new TelemetrySignal
        {
            Category = SignalCategory.Exception,
            Name = exception.GetType().FullName ?? exception.GetType().Name,
            Severity = severity,
            Properties = Freeze(properties),
            Exception = ExceptionSnapshotFactory.Create(exception)
        });
    }

    /// <inheritdoc />
    public void TrackMetric(string name, double value, IReadOnlyDictionary<string, string>? properties = null)
        => Track(new TelemetrySignal
        {
            Category = SignalCategory.Metric,
            Name = Require(name),
            Severity = SignalSeverity.Information,
            Properties = Freeze(properties),
            Measurements = new ReadOnlyDictionary<string, double>(new Dictionary<string, double> { ["value"] = value })
        });

    /// <inheritdoc />
    public ITelemetryScope BeginOperation(string name, IReadOnlyDictionary<string, string>? properties = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new TelemetryScope(name, this, _timeProvider, properties);
    }

    /// <inheritdoc />
    public ValueTask FlushAsync(CancellationToken cancellationToken = default) => _exporter.FlushAsync(cancellationToken);

    private static string Require(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return name;
    }

    private static IReadOnlyDictionary<string, string> Freeze(IReadOnlyDictionary<string, string>? properties)
        => properties is null || properties.Count == 0
            ? ReadOnlyDictionary<string, string>.Empty
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(properties, StringComparer.Ordinal));

    private static IReadOnlyDictionary<string, double> FreezeMeasurements(IReadOnlyDictionary<string, double>? measurements)
        => measurements is null || measurements.Count == 0
            ? ReadOnlyDictionary<string, double>.Empty
            : new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(measurements, StringComparer.Ordinal));
}
