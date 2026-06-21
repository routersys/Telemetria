using System.Security.Cryptography;

namespace Telemetria.Security;

/// <summary>
/// ハイブリッド暗号化で用いる鍵ペアを表します。サーバー側の鍵生成を補助します。
/// </summary>
public sealed record HybridKeyPair
{
    /// <summary>公開鍵 (SubjectPublicKeyInfo) です。</summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>秘密鍵 (PKCS#8) です。</summary>
    public required byte[] PrivateKey { get; init; }

    /// <summary>新しい P-256 鍵ペアを生成します。</summary>
    public static HybridKeyPair Generate()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return new HybridKeyPair
        {
            PublicKey = ecdh.ExportSubjectPublicKeyInfo(),
            PrivateKey = ecdh.ExportPkcs8PrivateKey()
        };
    }
}
