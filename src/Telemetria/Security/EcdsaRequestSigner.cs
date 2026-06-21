using System.Security.Cryptography;

namespace Telemetria.Security;

/// <summary>
/// P-256 ECDSA を用いてペイロードに署名します。
/// TOTP と異なり、署名は送信内容に直接紐付くため、他のペイロードへの流用はできません。
/// </summary>
public sealed class EcdsaRequestSigner : IRequestSigner, IDisposable
{
    private readonly ECDsa _key;

    /// <summary>PKCS#8 形式の秘密鍵で初期化します。</summary>
    public EcdsaRequestSigner(byte[] pkcs8PrivateKey)
    {
        ArgumentNullException.ThrowIfNull(pkcs8PrivateKey);
        _key = ECDsa.Create();
        _key.ImportPkcs8PrivateKey(pkcs8PrivateKey, out _);
    }

    /// <inheritdoc />
    public string Sign(ReadOnlySpan<byte> payload)
        => Convert.ToBase64String(_key.SignData(payload, HashAlgorithmName.SHA256));

    /// <inheritdoc />
    public bool Verify(ReadOnlySpan<byte> payload, string signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        var buffer = new byte[((signature.Length + 3) / 4) * 3];
        if (!Convert.TryFromBase64String(signature, buffer, out var written))
        {
            return false;
        }

        return _key.VerifyData(payload, buffer.AsSpan(0, written), HashAlgorithmName.SHA256);
    }

    /// <inheritdoc />
    public void Dispose() => _key.Dispose();
}
