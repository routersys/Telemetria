namespace Telemetria;

/// <summary>
/// 操作の時間計測を行うテレメトリスコープを表します。破棄時に所要時間を含む信号を自動記録します。
/// </summary>
public interface ITelemetryScope : IDisposable, IAsyncDisposable
{
    /// <summary>スコープに文字列プロパティを追加します。</summary>
    ITelemetryScope AddProperty(string key, string value);

    /// <summary>スコープに数値計測値を追加します。</summary>
    ITelemetryScope AddMeasurement(string key, double value);

    /// <summary>スコープをエラーとしてマークします。破棄時に例外信号として記録されます。</summary>
    void Fail(Exception exception);
}
