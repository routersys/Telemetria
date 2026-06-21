using Microsoft.Extensions.DependencyInjection;
using Telemetria.Hosting;
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

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
