namespace Telemetria;

/// <summary>
/// 平文を暗号化して保護済みペイロードを生成する処理を表します。
/// </summary>
public interface IPayloadProtector
{
    /// <summary>平文を暗号化します。</summary>
    ProtectedPayload Protect(ReadOnlySpan<byte> plaintext);
}
