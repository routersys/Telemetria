using System.Security.Cryptography;

namespace Telemetria.Security;

/// <summary>
/// 個人を特定しないランダムな匿名識別子を提供します。
/// </summary>
public sealed class AnonymousIdentityProvider : IAnonymousIdentityProvider
{
    private readonly Lock _gate = new();
    private string _current;

    /// <summary>新しい匿名識別子を割り当てて初期化します。</summary>
    public AnonymousIdentityProvider()
    {
        _current = NewIdentity();
    }

    /// <inheritdoc />
    public string Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public void Rotate()
    {
        lock (_gate)
        {
            _current = NewIdentity();
        }
    }

    private static string NewIdentity()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Base32.Encode(bytes).TrimEnd('=');
    }
}
