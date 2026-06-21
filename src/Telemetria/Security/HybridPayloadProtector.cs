using System.Security.Cryptography;

namespace Telemetria.Security;

/// <summary>
/// 一時鍵による楕円曲線ディフィー・ヘルマン鍵共有と AES-256-GCM を組み合わせた
/// ハイブリッド暗号化でペイロードを保護します。
/// </summary>
public sealed class HybridPayloadProtector : IPayloadProtector
{
    private readonly byte[] _serverPublicKey;
    private readonly string _keyId;

    /// <summary>サーバー公開鍵 (SubjectPublicKeyInfo) と鍵識別子で初期化します。</summary>
    public HybridPayloadProtector(byte[] serverPublicKeySubjectPublicKeyInfo, string keyId)
    {
        ArgumentNullException.ThrowIfNull(serverPublicKeySubjectPublicKeyInfo);
        ArgumentException.ThrowIfNullOrEmpty(keyId);

        _serverPublicKey = serverPublicKeySubjectPublicKeyInfo;
        _keyId = keyId;
    }

    /// <inheritdoc />
    public ProtectedPayload Protect(ReadOnlySpan<byte> plaintext)
    {
        using var ephemeral = HybridCrypto.CreateEphemeral();
        var serverKey = HybridCrypto.ImportPublicKey(_serverPublicKey);
        var key = HybridCrypto.DeriveKey(ephemeral, serverKey);

        try
        {
            var nonce = new byte[HybridCrypto.NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var cipher = new byte[plaintext.Length];
            var tag = new byte[HybridCrypto.TagSize];

            using (var aes = new AesGcm(key, HybridCrypto.TagSize))
            {
                aes.Encrypt(nonce, plaintext, cipher, tag);
            }

            return new ProtectedPayload
            {
                KeyId = _keyId,
                EphemeralPublicKey = ephemeral.ExportSubjectPublicKeyInfo(),
                Nonce = nonce,
                CipherText = cipher,
                Tag = tag
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
