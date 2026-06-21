namespace Telemetria;

/// <summary>
/// バッファが満杯のときの挙動を表します。
/// </summary>
public enum BufferFullMode
{
    /// <summary>新しい信号を破棄します。</summary>
    DropNewest,

    /// <summary>最も古い信号を破棄して書き込みます。</summary>
    DropOldest
}
