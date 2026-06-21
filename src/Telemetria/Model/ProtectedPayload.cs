namespace Telemetria;

/// <summary>
/// ハイブリッド暗号化によって保護されたペイロードを表します。
/// </summary>
public sealed record ProtectedPayload
{
    /// <summary>暗号化スキームのバージョンです。</summary>
    public int Version { get; init; } = 1;

    /// <summary>復号に用いるサーバー鍵の識別子です。</summary>
    public required string KeyId { get; init; }

    /// <summary>鍵共有に用いる一時公開鍵 (SubjectPublicKeyInfo) です。</summary>
    public required byte[] EphemeralPublicKey { get; init; }

    /// <summary>AES-GCM のノンスです。</summary>
    public required byte[] Nonce { get; init; }

    /// <summary>暗号文です。</summary>
    public required byte[] CipherText { get; init; }

    /// <summary>AES-GCM の認証タグです。</summary>
    public required byte[] Tag { get; init; }
}
