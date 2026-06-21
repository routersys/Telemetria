namespace Telemetria;

/// <summary>
/// 保護済みペイロードを復号する処理を表します。受信側で利用します。
/// </summary>
public interface IPayloadOpener
{
    /// <summary>保護済みペイロードを復号して平文を取得します。</summary>
    byte[] Open(ProtectedPayload payload);
}
