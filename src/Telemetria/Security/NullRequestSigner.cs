namespace Telemetria.Security;

internal sealed class NullRequestSigner : IRequestSigner
{
    public static NullRequestSigner Instance { get; } = new();

    private NullRequestSigner() { }

    public string Sign(ReadOnlySpan<byte> payload) => string.Empty;

    public bool Verify(ReadOnlySpan<byte> payload, string signature) => false;
}
