namespace Telemetria;

/// <summary>
/// ローカル保管された再送待ちのバッチを表します。
/// </summary>
public sealed record StoredBatch
{
    /// <summary>保管された項目を一意に識別するトークンです。</summary>
    public required string Token { get; init; }

    /// <summary>保管されたバッチです。</summary>
    public required SignalBatch Batch { get; init; }
}
