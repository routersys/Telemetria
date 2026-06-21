using System.Collections.Concurrent;
using System.Net;

namespace Telemetria.Tests.Support;

internal sealed class NoopRequestSigner : IRequestSigner
{
    public static NoopRequestSigner Instance { get; } = new();

    private NoopRequestSigner() { }

    public string Sign(ReadOnlySpan<byte> payload) => string.Empty;

    public bool Verify(ReadOnlySpan<byte> payload, string signature) => false;
}

internal sealed class CapturingSink : ITelemetrySink
{
    private readonly SinkResult _result;

    public CapturingSink(SinkResult? result = null)
    {
        _result = result ?? SinkResult.Success;
    }

    public ConcurrentQueue<SignalBatch> Batches { get; } = new();

    public int ExportCount { get; private set; }

    public ValueTask<SinkResult> ExportAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        Batches.Enqueue(batch);
        ExportCount++;
        return ValueTask.FromResult(_result);
    }
}

internal sealed class StubTransport : ITelemetryTransport
{
    private readonly Func<ProtectedEnvelope, bool> _behavior;

    public StubTransport(Func<ProtectedEnvelope, bool> behavior)
    {
        _behavior = behavior;
    }

    public List<ProtectedEnvelope> Sent { get; } = [];

    public ValueTask<bool> SendAsync(ProtectedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        Sent.Add(envelope);
        return ValueTask.FromResult(_behavior(envelope));
    }
}

internal sealed class InMemoryLocalStore : ILocalSignalStore
{
    private readonly ConcurrentDictionary<string, SignalBatch> _items = new();
    private int _sequence;

    public int Count => _items.Count;

    public ValueTask StoreAsync(SignalBatch batch, CancellationToken cancellationToken = default)
    {
        var token = Interlocked.Increment(ref _sequence).ToString("D6");
        _items[token] = batch;
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<StoredBatch> ReadPendingAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (token, batch) in _items.OrderBy(static kv => kv.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new StoredBatch { Token = token, Batch = batch };
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    public ValueTask RemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        _items.TryRemove(token, out _);
        return ValueTask.CompletedTask;
    }
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<byte[]> Bodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (request.Content is not null)
        {
            Bodies.Add(await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
        }

        return _responder(request);
    }
}

internal sealed class SingleClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public SingleClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}

internal static class StatusResponses
{
    public static Func<HttpRequestMessage, HttpResponseMessage> WithStatus(HttpStatusCode status)
        => _ => new HttpResponseMessage(status);
}
