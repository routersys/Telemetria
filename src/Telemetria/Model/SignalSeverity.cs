namespace Telemetria;

/// <summary>
/// テレメトリ信号の重大度を表します。
/// </summary>
public enum SignalSeverity
{
    /// <summary>最も詳細な追跡情報です。</summary>
    Trace,

    /// <summary>デバッグ向けの情報です。</summary>
    Debug,

    /// <summary>通常の情報です。</summary>
    Information,

    /// <summary>警告です。</summary>
    Warning,

    /// <summary>エラーです。</summary>
    Error,

    /// <summary>致命的なエラーです。</summary>
    Critical
}
