namespace Telemetria;

/// <summary>
/// 匿名識別子とワンタイムパスワードを伴う、送信単位の保護済みエンベロープを表します。
/// </summary>
public sealed record ProtectedEnvelope
{
    /// <summary>エンベロープのスキーマバージョンです。</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>個人を特定しない匿名識別子です。</summary>
    public required string AnonymousId { get; init; }

    /// <summary>送信時に検証されるワンタイムパスワードです。</summary>
    public required string OneTimePassword { get; init; }

    /// <summary>暗号化されたペイロードです。</summary>
    public required ProtectedPayload Payload { get; init; }

    /// <summary>
    /// ペイロードの ECDSA 署名です (省略可能)。
    /// 設定された場合、サーバーは登録済みの公開鍵でこの署名を検証できます。
    /// </summary>
    public string? RequestSignature { get; init; }

    /// <summary>エンベロープが生成された時刻です。</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
