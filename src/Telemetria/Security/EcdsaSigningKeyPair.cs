using System.Security.Cryptography;

namespace Telemetria.Security;

/// <summary>
/// P-256 ECDSA で用いる署名鍵ペアを表します。クライアント側の鍵生成を補助します。
/// </summary>
public sealed record EcdsaSigningKeyPair
{
    /// <summary>秘密鍵 (PKCS#8) です。</summary>
    public required byte[] PrivateKey { get; init; }

    /// <summary>公開鍵 (SubjectPublicKeyInfo) です。</summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>秘密鍵を Base64 エンコードした文字列です。</summary>
    public string PrivateKeyBase64 => Convert.ToBase64String(PrivateKey);

    /// <summary>公開鍵を Base64 エンコードした文字列です。サーバーへの登録に用います。</summary>
    public string PublicKeyBase64 => Convert.ToBase64String(PublicKey);

    /// <summary>新しい P-256 ECDSA 署名鍵ペアを生成します。</summary>
    public static EcdsaSigningKeyPair Generate()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new EcdsaSigningKeyPair
        {
            PrivateKey = ecdsa.ExportPkcs8PrivateKey(),
            PublicKey = ecdsa.ExportSubjectPublicKeyInfo()
        };
    }
}
