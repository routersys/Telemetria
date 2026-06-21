using System.Collections.ObjectModel;

namespace Telemetria.Collection;

internal sealed class TelemetryScope : ITelemetryScope
{
    private readonly string _name;
    private readonly ITelemetryClient _client;
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _startedAt;
    private readonly Dictionary<string, string> _properties;
    private readonly Dictionary<string, double> _measurements = [];
    private Exception? _exception;
    private int _disposed;

    internal TelemetryScope(
        string name,
        ITelemetryClient client,
        TimeProvider timeProvider,
        IReadOnlyDictionary<string, string>? properties)
    {
        _name = name;
        _client = client;
        _timeProvider = timeProvider;
        _startedAt = timeProvider.GetUtcNow();
        _properties = properties is null or { Count: 0 }
            ? []
            : new Dictionary<string, string>(properties, StringComparer.Ordinal);
    }

    public ITelemetryScope AddProperty(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _properties[key] = value;
        return this;
    }

    public ITelemetryScope AddMeasurement(string key, double value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _measurements[key] = value;
        return this;
    }

    public void Fail(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _exception = exception;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Record();
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void Record()
    {
        _measurements["duration_ms"] = (_timeProvider.GetUtcNow() - _startedAt).TotalMilliseconds;

        _client.Track(new TelemetrySignal
        {
            Category = _exception is null ? SignalCategory.Diagnostic : SignalCategory.Exception,
            Name = _name,
            Severity = _exception is null ? SignalSeverity.Information : SignalSeverity.Error,
            Timestamp = _startedAt,
            Properties = _properties.Count > 0
                ? new ReadOnlyDictionary<string, string>(_properties)
                : ReadOnlyDictionary<string, string>.Empty,
            Measurements = new ReadOnlyDictionary<string, double>(_measurements),
            Exception = _exception is not null ? ExceptionSnapshotFactory.Create(_exception) : null
        });
    }
}
