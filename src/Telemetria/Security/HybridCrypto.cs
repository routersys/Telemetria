using System.Security.Cryptography;

namespace Telemetria.Security;

internal static class HybridCrypto
{
    internal const int NonceSize = 12;
    internal const int TagSize = 16;
    internal const int KeySize = 32;

    private static readonly byte[] DerivationInfo = System.Text.Encoding.UTF8.GetBytes("Telemetria/hybrid/v1");

    internal static ECDiffieHellman CreateEphemeral() => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

    internal static byte[] DeriveKey(ECDiffieHellman self, ECDiffieHellmanPublicKey other)
    {
        var shared = self.DeriveRawSecretAgreement(other);
        try
        {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, shared, KeySize, salt: null, info: DerivationInfo);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(shared);
        }
    }

    internal static ECDiffieHellmanPublicKey ImportPublicKey(byte[] subjectPublicKeyInfo)
    {
        var imported = ECDiffieHellman.Create();
        imported.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
        return imported.PublicKey;
    }
}
