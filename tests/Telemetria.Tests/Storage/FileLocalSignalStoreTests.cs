using Microsoft.Extensions.Time.Testing;
using Telemetria.Serialization;
using Telemetria.Storage;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Storage;

public sealed class FileLocalSignalStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "telemetria-store-" + Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.UnixEpoch.AddSeconds(1));

    private FileLocalSignalStore Create(int maxItems = 1024)
        => new(
            new JsonSignalSerializer(),
            new StaticOptionsMonitor<LocalStoreOptions>(new LocalStoreOptions { Directory = _directory, MaxItems = maxItems }),
            _clock);

    private static SignalBatch Batch(string name)
        => new()
        {
            CreatedAt = DateTimeOffset.UnixEpoch,
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = name }]
        };

    private async Task<List<StoredBatch>> ReadAllAsync(FileLocalSignalStore store)
    {
        var list = new List<StoredBatch>();
        await foreach (var item in store.ReadPendingAsync())
        {
            list.Add(item);
        }

        return list;
    }

    [Fact]
    public async Task StoreThenRead_RoundTrips()
    {
        var store = Create();
        await store.StoreAsync(Batch("a"));
        _clock.Advance(TimeSpan.FromSeconds(1));
        await store.StoreAsync(Batch("b"));

        var pending = await ReadAllAsync(store);

        Assert.Equal(2, pending.Count);
        Assert.Equal(["a", "b"], pending.Select(p => p.Batch.Signals[0].Name));
    }

    [Fact]
    public async Task Remove_DeletesStoredItem()
    {
        var store = Create();
        await store.StoreAsync(Batch("a"));
        var pending = await ReadAllAsync(store);

        await store.RemoveAsync(pending[0].Token);

        Assert.Empty(await ReadAllAsync(store));
    }

    [Fact]
    public async Task ReadPending_EmptyDirectory_ReturnsNothing()
    {
        var store = Create();
        Assert.Empty(await ReadAllAsync(store));
    }

    [Fact]
    public async Task Store_BeyondMaxItems_EvictsOldest()
    {
        var store = Create(maxItems: 2);
        await store.StoreAsync(Batch("a"));
        _clock.Advance(TimeSpan.FromSeconds(1));
        await store.StoreAsync(Batch("b"));
        _clock.Advance(TimeSpan.FromSeconds(1));
        await store.StoreAsync(Batch("c"));

        var pending = await ReadAllAsync(store);

        Assert.Equal(2, pending.Count);
        Assert.DoesNotContain(pending, p => p.Batch.Signals[0].Name == "a");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
