namespace Telemetria;

/// <summary>
/// テレメトリ信号の分類を表します。
/// </summary>
public enum SignalCategory
{
    /// <summary>利用状況を示す信号です。</summary>
    Usage,

    /// <summary>診断目的の信号です。</summary>
    Diagnostic,

    /// <summary>処理上のエラーを示す信号です。</summary>
    Error,

    /// <summary>例外の発生を示す信号です。</summary>
    Exception,

    /// <summary>数値計測を示す信号です。</summary>
    Metric,

    /// <summary>呼び出し側が独自に定義する信号です。</summary>
    Custom
}
