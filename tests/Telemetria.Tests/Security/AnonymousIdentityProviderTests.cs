using Telemetria.Security;
using Xunit;

namespace Telemetria.Tests.Security;

public sealed class AnonymousIdentityProviderTests
{
    [Fact]
    public void Current_IsStableUntilRotated()
    {
        var provider = new AnonymousIdentityProvider();
        var first = provider.Current;
        Assert.Equal(first, provider.Current);
    }

    [Fact]
    public void Rotate_ChangesIdentity()
    {
        var provider = new AnonymousIdentityProvider();
        var before = provider.Current;
        provider.Rotate();
        Assert.NotEqual(before, provider.Current);
    }

    [Fact]
    public void Current_IsNonEmptyBase32()
    {
        var provider = new AnonymousIdentityProvider();
        Assert.False(string.IsNullOrWhiteSpace(provider.Current));
        Assert.DoesNotContain('=', provider.Current);
        Assert.All(provider.Current, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));
    }
}
