namespace Telemetria;

/// <summary>
/// バッチを暗号化し、匿名識別子とワンタイムパスワードを付与してエンベロープを生成します。
/// </summary>
public interface IEnvelopeFactory
{
    /// <summary>バッチから保護済みエンベロープを生成します。</summary>
    ProtectedEnvelope Create(SignalBatch batch);
}
