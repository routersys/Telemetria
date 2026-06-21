namespace Telemetria;

/// <summary>
/// バッチをバイト列へ直列化、またはバイト列から復元する処理を表します。
/// </summary>
public interface ISignalSerializer
{
    /// <summary>バッチをバイト列へ直列化します。</summary>
    byte[] Serialize(SignalBatch batch);

    /// <summary>バイト列からバッチを復元します。</summary>
    SignalBatch Deserialize(ReadOnlySpan<byte> data);
}
