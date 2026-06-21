namespace Telemetria;

/// <summary>
/// テレメトリ信号を記録するための主要な入口を表します。
/// </summary>
public interface ITelemetryClient
{
    /// <summary>あらかじめ構築済みの信号を記録します。</summary>
    void Track(TelemetrySignal signal);

    /// <summary>利用状況を記録します。</summary>
    void TrackUsage(string name, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, double>? measurements = null);

    /// <summary>エラーを記録します。</summary>
    void TrackError(string name, SignalSeverity severity = SignalSeverity.Error, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>例外を解析して記録します。</summary>
    void TrackException(Exception exception, SignalSeverity severity = SignalSeverity.Error, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>数値計測値を記録します。</summary>
    void TrackMetric(string name, double value, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>操作の開始を宣言し、破棄時に所要時間を自動記録するスコープを返します。</summary>
    ITelemetryScope BeginOperation(string name, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>未送信の信号を確実に送出します。</summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
