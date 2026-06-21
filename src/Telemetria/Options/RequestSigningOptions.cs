namespace Telemetria;

/// <summary>
/// ECDSA リクエスト署名の動作を構成するオプションです。
/// </summary>
public sealed class RequestSigningOptions
{
    /// <summary>P-256 ECDSA 秘密鍵 (PKCS#8) を Base64 エンコードした文字列です。</summary>
    public string PrivateKeyBase64 { get; set; } = string.Empty;
}
