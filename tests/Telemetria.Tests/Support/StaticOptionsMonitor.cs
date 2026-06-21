using Microsoft.Extensions.Options;

namespace Telemetria.Tests.Support;

internal sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class
{
    public StaticOptionsMonitor(T value)
    {
        CurrentValue = value;
    }

    public T CurrentValue { get; private set; }

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string?> listener) => NoopDisposable.Instance;

    public void Set(T value) => CurrentValue = value;

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
