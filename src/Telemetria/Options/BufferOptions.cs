namespace Telemetria;

/// <summary>
/// 信号バッファの構成オプションです。
/// </summary>
public sealed class BufferOptions
{
    /// <summary>バッファに保持できる信号の最大件数です。</summary>
    public int Capacity { get; set; } = 4096;

    /// <summary>バッファが満杯のときの挙動です。</summary>
    public BufferFullMode FullMode { get; set; } = BufferFullMode.DropOldest;
}
