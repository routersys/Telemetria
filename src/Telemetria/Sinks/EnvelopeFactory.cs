namespace Telemetria.Sinks;

/// <summary>
/// バッチを直列化・暗号化し、匿名識別子とワンタイムパスワードを付与してエンベロープを生成します。
/// </summary>
public sealed class EnvelopeFactory : IEnvelopeFactory
{
    private readonly ISignalSerializer _serializer;
    private readonly IPayloadProtector _protector;
    private readonly IOneTimePasswordProvider _oneTimePassword;
    private readonly IAnonymousIdentityProvider _identity;
    private readonly TimeProvider _timeProvider;

    /// <summary>依存関係を指定して初期化します。</summary>
    public EnvelopeFactory(
        ISignalSerializer serializer,
        IPayloadProtector protector,
        IOneTimePasswordProvider oneTimePassword,
        IAnonymousIdentityProvider identity,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(oneTimePassword);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _serializer = serializer;
        _protector = protector;
        _oneTimePassword = oneTimePassword;
        _identity = identity;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public ProtectedEnvelope Create(SignalBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var plaintext = _serializer.Serialize(batch);
        var payload = _protector.Protect(plaintext);

        return new ProtectedEnvelope
        {
            AnonymousId = _identity.Current,
            OneTimePassword = _oneTimePassword.Generate(),
            Payload = payload,
            CreatedAt = _timeProvider.GetUtcNow()
        };
    }
}
