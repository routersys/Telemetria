using Microsoft.Extensions.DependencyInjection;
using Telemetria.Hosting;
using Telemetria.Pipeline;
using Telemetria.Security;
using Telemetria.Sinks;
using Xunit;

namespace Telemetria.Tests.Hosting;

public sealed class DependencyInjectionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "telemetria-di-" + Guid.NewGuid().ToString("N"));

    private static ServiceProvider Build(Action<TelemetriaBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTelemetria(configure);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddTelemetria_ResolvesClientAndPipeline()
    {
        using var provider = Build(builder => builder.UseLocalFile(o => o.Directory = _directory));

        Assert.NotNull(provider.GetRequiredService<ITelemetryClient>());
        Assert.NotNull(provider.GetRequiredService<ISignalPipeline>());
        Assert.NotNull(provider.GetRequiredService<ISignalExporter>());
    }

    [Fact]
    public void AddTelemetria_NoSinkConfigured_ResolvesNullSink()
    {
        using var provider = Build(_ => { });
        Assert.IsType<NullTelemetrySink>(provider.GetRequiredService<ITelemetrySink>());
    }

    [Fact]
    public void AddTelemetria_LocalFile_ResolvesFileSink()
    {
        using var provider = Build(builder => builder.UseLocalFile(o => o.Directory = _directory));
        Assert.IsType<FileTelemetrySink>(provider.GetRequiredService<ITelemetrySink>());
    }

    [Fact]
    public void AddTelemetria_Remote_ResolvesFailoverSinkAndSecurity()
    {
        var keyPair = HybridKeyPair.Generate();
        using var provider = Build(builder => builder.UseRemote(
            transport => transport.Endpoint = new Uri("https://example.invalid/ingest"),
            encryption => encryption.ServerPublicKeyBase64 = Convert.ToBase64String(keyPair.PublicKey)));

        Assert.IsType<FailoverTelemetrySink>(provider.GetRequiredService<ITelemetrySink>());
        Assert.NotNull(provider.GetRequiredService<IPayloadProtector>());
        Assert.NotNull(provider.GetRequiredService<IOneTimePasswordProvider>());
        Assert.NotNull(provider.GetRequiredService<ITelemetryTransport>());
    }

    [Fact]
    public void AddTelemetria_Both_ResolvesCompositeSink()
    {
        var keyPair = HybridKeyPair.Generate();
        using var provider = Build(builder =>
        {
            builder.UseRemote(
                transport => transport.Endpoint = new Uri("https://example.invalid/ingest"),
                encryption => encryption.ServerPublicKeyBase64 = Convert.ToBase64String(keyPair.PublicKey));
            builder.UseLocalFile(o => o.Directory = _directory);
        });

        Assert.IsType<CompositeTelemetrySink>(provider.GetRequiredService<ITelemetrySink>());
    }

    [Fact]
    public async Task EndToEnd_LocalFile_AnonymizesAndWrites()
    {
        using var provider = Build(builder =>
        {
            builder.ConfigureCore(o =>
            {
                o.Mode = TelemetriaMode.Local;
                o.ApplicationName = "ItApp";
            });
            builder.UseLocalFile(o => o.Directory = _directory);
        });

        var client = provider.GetRequiredService<ITelemetryClient>();
        client.TrackUsage("opened", new Dictionary<string, string> { ["contact"] = "alice@example.com" });
        await client.FlushAsync();

        var files = Directory.GetFiles(_directory, "*.ndjson");
        var content = await File.ReadAllTextAsync(Assert.Single(files));

        Assert.Contains("opened", content);
        Assert.Contains("ItApp", content);
        Assert.Contains("[email]", content);
        Assert.DoesNotContain("alice@example.com", content);
    }

    [Fact]
    public void AddTelemetria_Memory_ResolvesInMemorySink()
    {
        using var provider = Build(builder => builder.UseMemory());
        Assert.IsType<InMemoryTelemetrySink>(provider.GetRequiredService<ITelemetrySink>());
    }

    [Fact]
    public void AddTelemetria_MemoryAndLocalFile_ResolvesCompositeSink()
    {
        using var provider = Build(builder =>
        {
            builder.UseMemory();
            builder.UseLocalFile(o => o.Directory = _directory);
        });
        Assert.IsType<CompositeTelemetrySink>(provider.GetRequiredService<ITelemetrySink>());
    }

    [Fact]
    public void AddTelemetria_Memory_SinkIsAccessibleDirectly()
    {
        using var provider = Build(builder => builder.UseMemory(500));
        Assert.NotNull(provider.GetRequiredService<InMemoryTelemetrySink>());
    }

    [Fact]
    public void AddTelemetria_UseMemory_ZeroCapacity_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Build(builder => builder.UseMemory(0)));

    [Fact]
    public void AddDeduplication_RegistersProcessor()
    {
        using var provider = Build(builder => builder.AddDeduplication());
        var processors = provider.GetServices<ISignalProcessor>().ToList();
        Assert.Contains(processors, p => p is DeduplicationProcessor);
    }

    [Fact]
    public void AddDeduplication_WithOptions_ConfiguresWindow()
    {
        using var provider = Build(builder =>
            builder.AddDeduplication(o => o.Window = TimeSpan.FromSeconds(30)));

        var opts = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeduplicationOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(30), opts.Window);
    }

    [Fact]
    public void AddTelemetria_ResolvesScope_ViaBeginOperation()
    {
        using var provider = Build(_ => { });
        var client = provider.GetRequiredService<ITelemetryClient>();
        using var scope = client.BeginOperation("test-op");
        Assert.NotNull(scope);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
