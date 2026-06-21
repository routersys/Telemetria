using System.Security.Cryptography;

namespace Telemetria.Security;

/// <summary>
/// <see cref="HybridPayloadProtector"/> で保護されたペイロードを、サーバー秘密鍵を用いて復号します。
/// </summary>
public sealed class HybridPayloadOpener : IPayloadOpener, IDisposable
{
    private readonly ECDiffieHellman _serverKey;

    /// <summary>サーバー秘密鍵 (PKCS#8) で初期化します。</summary>
    public HybridPayloadOpener(byte[] serverPrivateKeyPkcs8)
    {
        ArgumentNullException.ThrowIfNull(serverPrivateKeyPkcs8);

        _serverKey = ECDiffieHellman.Create();
        _serverKey.ImportPkcs8PrivateKey(serverPrivateKeyPkcs8, out _);
    }

    /// <inheritdoc />
    public byte[] Open(ProtectedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var ephemeral = HybridCrypto.ImportPublicKey(payload.EphemeralPublicKey);
        var key = HybridCrypto.DeriveKey(_serverKey, ephemeral);

        try
        {
            var plaintext = new byte[payload.CipherText.Length];
            using var aes = new AesGcm(key, HybridCrypto.TagSize);
            aes.Decrypt(payload.Nonce, payload.CipherText, payload.Tag, plaintext);
            return plaintext;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>保持しているサーバー秘密鍵を解放します。</summary>
    public void Dispose() => _serverKey.Dispose();
}
