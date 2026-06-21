using Microsoft.Extensions.Options;
using Telemetria.Buffering;
using Xunit;

namespace Telemetria.Tests.Buffering;

public sealed class ChannelSignalBufferTests
{
    private static TelemetrySignal Signal(string name = "s")
        => new() { Category = SignalCategory.Usage, Name = name };

    private static ChannelSignalBuffer Create(int capacity, BufferFullMode mode)
        => new(Options.Create(new BufferOptions { Capacity = capacity, FullMode = mode }));

    [Fact]
    public void TryWrite_WithinCapacity_Succeeds()
    {
        var buffer = Create(4, BufferFullMode.DropNewest);
        Assert.True(buffer.TryWrite(Signal()));
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void TryWrite_DropNewest_WhenFull_ReturnsFalse()
    {
        var buffer = Create(2, BufferFullMode.DropNewest);
        Assert.True(buffer.TryWrite(Signal("a")));
        Assert.True(buffer.TryWrite(Signal("b")));
        Assert.False(buffer.TryWrite(Signal("c")));
        Assert.Equal(2, buffer.Count);
    }

    [Fact]
    public void TryWrite_DropOldest_WhenFull_EvictsOldest()
    {
        var buffer = Create(2, BufferFullMode.DropOldest);
        Assert.True(buffer.TryWrite(Signal("a")));
        Assert.True(buffer.TryWrite(Signal("b")));
        Assert.True(buffer.TryWrite(Signal("c")));

        Assert.True(buffer.TryReadBatch(10, out var batch));
        Assert.Equal(["b", "c"], batch.Select(s => s.Name));
    }

    [Fact]
    public void TryReadBatch_Empty_ReturnsFalse()
    {
        var buffer = Create(2, BufferFullMode.DropNewest);
        Assert.False(buffer.TryReadBatch(10, out var batch));
        Assert.Empty(batch);
    }

    [Fact]
    public void TryReadBatch_RespectsMaxSize()
    {
        var buffer = Create(8, BufferFullMode.DropNewest);
        for (var i = 0; i < 5; i++)
        {
            buffer.TryWrite(Signal(i.ToString()));
        }

        Assert.True(buffer.TryReadBatch(3, out var batch));
        Assert.Equal(3, batch.Count);
        Assert.Equal(2, buffer.Count);
    }

    [Fact]
    public async Task ReadBatchAsync_ReturnsAvailableSignals()
    {
        var buffer = Create(8, BufferFullMode.DropNewest);
        buffer.TryWrite(Signal("a"));
        buffer.TryWrite(Signal("b"));

        var batch = await buffer.ReadBatchAsync(10, TimeSpan.FromMilliseconds(50), CancellationToken.None);

        Assert.Equal(2, batch.Count);
    }

    [Fact]
    public async Task ReadBatchAsync_StopsAtMaxBatchSize()
    {
        var buffer = Create(8, BufferFullMode.DropNewest);
        for (var i = 0; i < 6; i++)
        {
            buffer.TryWrite(Signal(i.ToString()));
        }

        var batch = await buffer.ReadBatchAsync(4, TimeSpan.FromSeconds(1), CancellationToken.None);

        Assert.Equal(4, batch.Count);
    }

    [Fact]
    public async Task ReadBatchAsync_AfterComplete_ReturnsEmpty()
    {
        var buffer = Create(8, BufferFullMode.DropNewest);
        buffer.Complete();

        var batch = await buffer.ReadBatchAsync(4, TimeSpan.FromMilliseconds(50), CancellationToken.None);

        Assert.Empty(batch);
    }

    [Fact]
    public async Task ReadBatchAsync_Cancellation_Propagates()
    {
        var buffer = Create(8, BufferFullMode.DropNewest);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await buffer.ReadBatchAsync(4, TimeSpan.FromSeconds(5), cts.Token));
    }
}
