namespace Telemetria;

/// <summary>
/// 例外の不変なスナップショットを表します。送信に適した形へ正規化された例外情報です。
/// </summary>
public sealed record ExceptionSnapshot
{
    /// <summary>例外型の完全名です。</summary>
    public required string Type { get; init; }

    /// <summary>例外メッセージです。</summary>
    public required string Message { get; init; }

    /// <summary>スタックトレースです。</summary>
    public string? StackTrace { get; init; }

    /// <summary>内側の例外のスナップショットです。</summary>
    public ExceptionSnapshot? Inner { get; init; }

    /// <summary>集約例外に含まれる個々の例外のスナップショットです。</summary>
    public IReadOnlyList<ExceptionSnapshot> Aggregated { get; init; } = [];
}
