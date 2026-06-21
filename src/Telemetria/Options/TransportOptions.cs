namespace Telemetria;

/// <summary>
/// HTTP 送信の構成オプションです。
/// </summary>
public sealed class TransportOptions
{
    /// <summary>送信先のエンドポイントです。</summary>
    public Uri? Endpoint { get; set; }

    /// <summary>送信のタイムアウトです。</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>ワンタイムパスワードを格納する HTTP ヘッダー名です。</summary>
    public string OneTimePasswordHeader { get; set; } = "X-Telemetria-Otp";

    /// <summary>匿名識別子を格納する HTTP ヘッダー名です。</summary>
    public string AnonymousIdHeader { get; set; } = "X-Telemetria-Anonymous-Id";

    /// <summary>ECDSA リクエスト署名を格納する HTTP ヘッダー名です。</summary>
    public string SignatureHeader { get; set; } = "X-Telemetria-Signature";
}
