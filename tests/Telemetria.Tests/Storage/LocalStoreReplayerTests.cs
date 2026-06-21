using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Telemetria.Security;
using Telemetria.Serialization;
using Telemetria.Sinks;
using Telemetria.Storage;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Storage;

public sealed class LocalStoreReplayerTests
{
    private static EnvelopeFactory Factory()
    {
        var keyPair = HybridKeyPair.Generate();
        var clock = new FakeTimeProvider();
        return new EnvelopeFactory(
            new JsonSignalSerializer(),
            new HybridPayloadProtector(keyPair.PublicKey, "key-1"),
            new TotpProvider(new OneTimePasswordOptions(), clock),
            new AnonymousIdentityProvider(),
            NoopRequestSigner.Instance,
            clock);
    }

    private static SignalBatch Batch(string name)
        => new()
        {
            CreatedAt = DateTimeOffset.UnixEpoch,
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = name }]
        };

    [Fact]
    public async Task ReplayAsync_AllAccepted_RemovesAllAndReturnsCount()
    {
        var store = new InMemoryLocalStore();
        await store.StoreAsync(Batch("a"));
        await store.StoreAsync(Batch("b"));

        var transport = new StubTransport(_ => true);
        var replayer = new LocalStoreReplayer(store, Factory(), transport, NullLogger<LocalStoreReplayer>.Instance);

        var replayed = await replayer.ReplayAsync(CancellationToken.None);

        Assert.Equal(2, replayed);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task ReplayAsync_TransportRejects_KeepsItemsAndStops()
    {
        var store = new InMemoryLocalStore();
        await store.StoreAsync(Batch("a"));
        await store.StoreAsync(Batch("b"));

        var transport = new StubTransport(_ => false);
        var replayer = new LocalStoreReplayer(store, Factory(), transport, NullLogger<LocalStoreReplayer>.Instance);

        var replayed = await replayer.ReplayAsync(CancellationToken.None);

        Assert.Equal(0, replayed);
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public async Task ReplayAsync_TransportThrows_StopsWithoutRemoving()
    {
        var store = new InMemoryLocalStore();
        await store.StoreAsync(Batch("a"));

        var transport = new StubTransport(_ => throw new InvalidOperationException("boom"));
        var replayer = new LocalStoreReplayer(store, Factory(), transport, NullLogger<LocalStoreReplayer>.Instance);

        var replayed = await replayer.ReplayAsync(CancellationToken.None);

        Assert.Equal(0, replayed);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task ReplayAsync_Empty_ReturnsZero()
    {
        var replayer = new LocalStoreReplayer(new InMemoryLocalStore(), Factory(), new StubTransport(_ => true), NullLogger<LocalStoreReplayer>.Instance);
        Assert.Equal(0, await replayer.ReplayAsync(CancellationToken.None));
    }
}
