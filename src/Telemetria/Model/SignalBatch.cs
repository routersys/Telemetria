namespace Telemetria;

/// <summary>
/// 一括で送信される複数のテレメトリ信号をまとめたバッチを表します。
/// </summary>
public sealed record SignalBatch
{
    /// <summary>バッチに含まれる信号の一覧です。</summary>
    public required IReadOnlyList<TelemetrySignal> Signals { get; init; }

    /// <summary>バッチが生成された時刻です。</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
