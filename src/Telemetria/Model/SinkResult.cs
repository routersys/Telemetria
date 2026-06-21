namespace Telemetria;

/// <summary>
/// シンクへの送信結果を表します。
/// </summary>
public readonly record struct SinkResult
{
    private SinkResult(bool succeeded, string? detail)
    {
        Succeeded = succeeded;
        Detail = detail;
    }

    /// <summary>送信が成功したかどうかを示します。</summary>
    public bool Succeeded { get; }

    /// <summary>結果に関する補足情報です。</summary>
    public string? Detail { get; }

    /// <summary>成功結果を取得します。</summary>
    public static SinkResult Success { get; } = new(true, null);

    /// <summary>指定した理由で失敗結果を生成します。</summary>
    public static SinkResult Failure(string detail) => new(false, detail);

    /// <summary>指定した理由で成功結果を生成します。</summary>
    public static SinkResult Deferred(string detail) => new(true, detail);
}
