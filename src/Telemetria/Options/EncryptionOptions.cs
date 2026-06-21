namespace Telemetria;

/// <summary>
/// ハイブリッド暗号化の構成オプションです。
/// </summary>
public sealed class EncryptionOptions
{
    /// <summary>復号に用いるサーバー鍵の識別子です。</summary>
    public string KeyId { get; set; } = "default";

    /// <summary>サーバーの公開鍵 (SubjectPublicKeyInfo) を Base64 で表したものです。</summary>
    public string? ServerPublicKeyBase64 { get; set; }
}
