namespace Telemetria;

/// <summary>
/// テレメトリ全体の動作を構成するオプションです。
/// </summary>
public sealed class TelemetriaOptions
{
    /// <summary>送出方式です。</summary>
    public TelemetriaMode Mode { get; set; } = TelemetriaMode.Local;

    /// <summary>計測対象のアプリケーション名です。</summary>
    public string ApplicationName { get; set; } = "unknown";

    /// <summary>計測対象のアプリケーションのバージョンです。</summary>
    public string ApplicationVersion { get; set; } = "0.0.0";

    /// <summary>この重大度未満の信号を破棄します。</summary>
    public SignalSeverity MinimumSeverity { get; set; } = SignalSeverity.Trace;

    /// <summary>0.0 から 1.0 の範囲で信号を採取する割合です。</summary>
    public double SamplingRate { get; set; } = 1.0;
}
