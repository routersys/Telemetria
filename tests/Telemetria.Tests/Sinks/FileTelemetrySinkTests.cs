using Microsoft.Extensions.Time.Testing;
using Telemetria.Serialization;
using Telemetria.Sinks;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Sinks;

public sealed class FileTelemetrySinkTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "telemetria-test-" + Guid.NewGuid().ToString("N"));

    private static SignalBatch Batch(string name)
        => new()
        {
            CreatedAt = DateTimeOffset.UnixEpoch,
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = name }]
        };

    [Fact]
    public async Task ExportAsync_CreatesDirectoryAndAppendsLines()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddDays(1));
        var sink = new FileTelemetrySink(
            new JsonSignalSerializer(),
            new StaticOptionsMonitor<FileSinkOptions>(new FileSinkOptions { Directory = _directory, FileNamePrefix = "sig" }),
            clock);

        await sink.ExportAsync(Batch("a"));
        await sink.ExportAsync(Batch("b"));

        var files = Directory.GetFiles(_directory, "*.ndjson");
        var file = Assert.Single(files);
        var lines = await File.ReadAllLinesAsync(file);
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"a\"", lines[0]);
        Assert.Contains("\"b\"", lines[1]);
    }

    [Fact]
    public async Task ExportAsync_ReturnsSuccess()
    {
        var sink = new FileTelemetrySink(
            new JsonSignalSerializer(),
            new StaticOptionsMonitor<FileSinkOptions>(new FileSinkOptions { Directory = _directory }),
            new FakeTimeProvider());

        var result = await sink.ExportAsync(Batch("a"));
        Assert.True(result.Succeeded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
