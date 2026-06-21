namespace Telemetria;

/// <summary>
/// 送出処理の構成オプションです。
/// </summary>
public sealed class DispatchOptions
{
    /// <summary>一度に送出するバッチの最大件数です。</summary>
    public int BatchSize { get; set; } = 64;

    /// <summary>バッチを構成するまでの最大待機時間です。</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>ローカル退避分を再送する間隔です。</summary>
    public TimeSpan ReplayInterval { get; set; } = TimeSpan.FromSeconds(30);
}
