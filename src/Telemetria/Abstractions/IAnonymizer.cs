namespace Telemetria;

/// <summary>
/// 信号から個人を特定し得る情報を取り除く処理を表します。
/// </summary>
public interface IAnonymizer
{
    /// <summary>信号を匿名化します。</summary>
    TelemetrySignal Anonymize(TelemetrySignal signal);
}
