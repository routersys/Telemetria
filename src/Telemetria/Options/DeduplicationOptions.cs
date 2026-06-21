namespace Telemetria;

/// <summary>
/// 重複信号の抑制動作を構成するオプションです。
/// </summary>
public sealed class DeduplicationOptions
{
    /// <summary>同一信号を抑制するウィンドウ幅です。</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>エラーおよび例外の信号を抑制の対象外とするかどうかです。</summary>
    public bool PassThroughErrors { get; set; } = true;
}
