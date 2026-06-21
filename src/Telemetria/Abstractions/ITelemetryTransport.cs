namespace Telemetria;

/// <summary>
/// 保護済みエンベロープを外部へ送信する低レベルの通信手段を表します。
/// </summary>
public interface ITelemetryTransport
{
    /// <summary>エンベロープを送信します。送信が受理された場合は <see langword="true"/> を返します。</summary>
    ValueTask<bool> SendAsync(ProtectedEnvelope envelope, CancellationToken cancellationToken = default);
}
