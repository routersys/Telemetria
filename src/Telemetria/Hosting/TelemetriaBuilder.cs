using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telemetria.Anonymization;
using Telemetria.Buffering;
using Telemetria.Collection;
using Telemetria.Enrichment;
using Telemetria.Pipeline;
using Telemetria.Security;
using Telemetria.Serialization;
using Telemetria.Sinks;
using Telemetria.Storage;
using Telemetria.Transport;

namespace Telemetria.Hosting;

/// <summary>
/// テレメトリの依存関係を構成するためのビルダーです。
/// </summary>
public sealed class TelemetriaBuilder
{
    private readonly List<Action<IServiceCollection>> _enricherRegistrations = [];
    private readonly List<Action<IServiceCollection>> _processorRegistrations = [];
    private bool _useRemote;
    private bool _useLocalFile;

    internal TelemetriaBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>基盤となるサービスコレクションです。</summary>
    public IServiceCollection Services { get; }

    /// <summary>中核となるオプションを構成します。</summary>
    public TelemetriaBuilder ConfigureCore(Action<TelemetriaOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }

    /// <summary>バッファのオプションを構成します。</summary>
    public TelemetriaBuilder ConfigureBuffer(Action<BufferOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }

    /// <summary>送出処理のオプションを構成します。</summary>
    public TelemetriaBuilder ConfigureDispatch(Action<DispatchOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }

    /// <summary>匿名化のオプションを構成します。</summary>
    public TelemetriaBuilder ConfigureAnonymization(Action<AnonymizationOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }

    /// <summary>付与処理を追加します。</summary>
    public TelemetriaBuilder AddEnricher<TEnricher>() where TEnricher : class, ISignalEnricher
    {
        _enricherRegistrations.Add(services => services.AddSingleton<ISignalEnricher, TEnricher>());
        return this;
    }

    /// <summary>付与処理のインスタンスを追加します。</summary>
    public TelemetriaBuilder AddEnricher(ISignalEnricher enricher)
    {
        ArgumentNullException.ThrowIfNull(enricher);
        _enricherRegistrations.Add(services => services.AddSingleton(enricher));
        return this;
    }

    /// <summary>パイプラインの処理を末尾に追加します。</summary>
    public TelemetriaBuilder AddProcessor<TProcessor>() where TProcessor : class, ISignalProcessor
    {
        _processorRegistrations.Add(services => services.AddSingleton<ISignalProcessor, TProcessor>());
        return this;
    }

    /// <summary>リモートサーバーへの暗号化送信を有効にします。</summary>
    public TelemetriaBuilder UseRemote(
        Action<TransportOptions> configureTransport,
        Action<EncryptionOptions> configureEncryption,
        Action<OneTimePasswordOptions>? configureOneTimePassword = null,
        Action<LocalStoreOptions>? configureLocalStore = null)
    {
        ArgumentNullException.ThrowIfNull(configureTransport);
        ArgumentNullException.ThrowIfNull(configureEncryption);

        Services.Configure(configureTransport);
        Services.Configure(configureEncryption);
        if (configureOneTimePassword is not null)
        {
            Services.Configure(configureOneTimePassword);
        }

        if (configureLocalStore is not null)
        {
            Services.Configure(configureLocalStore);
        }

        _useRemote = true;
        return this;
    }

    /// <summary>サーバーを用いないローカルファイルへの書き出しを有効にします。</summary>
    public TelemetriaBuilder UseLocalFile(Action<FileSinkOptions>? configure = null)
    {
        if (configure is not null)
        {
            Services.Configure(configure);
        }

        _useLocalFile = true;
        return this;
    }

    internal void Build()
    {
        RegisterCore();
        RegisterPipeline();
        RegisterRemote();
        RegisterLocalFile();
        RegisterSink();
        RegisterDispatcher();
    }

    private void RegisterCore()
    {
        Services.TryAddSingleton(TimeProvider.System);
        Services.TryAddSingleton<IAnonymousIdentityProvider, AnonymousIdentityProvider>();
        Services.TryAddSingleton<ISignalSerializer, JsonSignalSerializer>();
        Services.TryAddSingleton<IAnonymizer, DefaultAnonymizer>();
        Services.TryAddSingleton<ISignalBuffer, ChannelSignalBuffer>();
        Services.TryAddSingleton<ISignalExporter, SignalExporter>();
        Services.TryAddSingleton<ITelemetryClient, TelemetryClient>();
    }

    private void RegisterPipeline()
    {
        Services.AddSingleton<ISignalEnricher, RuntimeContextEnricher>();
        foreach (var registration in _enricherRegistrations)
        {
            registration(Services);
        }

        Services.AddSingleton<ISignalProcessor, SeverityFilterProcessor>();
        Services.AddSingleton<ISignalProcessor, SamplingProcessor>();
        Services.AddSingleton<ISignalProcessor, EnrichmentProcessor>();
        foreach (var registration in _processorRegistrations)
        {
            registration(Services);
        }

        Services.AddSingleton<ISignalProcessor, AnonymizationProcessor>();
        Services.TryAddSingleton<ISignalPipeline, SignalProcessorPipeline>();
    }

    private void RegisterRemote()
    {
        if (!_useRemote)
        {
            return;
        }

        Services.AddHttpClient(HttpTelemetryTransport.HttpClientName);
        Services.TryAddSingleton<IOneTimePasswordProvider>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OneTimePasswordOptions>>().Value;
            return new TotpProvider(options, sp.GetRequiredService<TimeProvider>());
        });
        Services.TryAddSingleton<IPayloadProtector>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EncryptionOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.ServerPublicKeyBase64))
            {
                throw new InvalidOperationException("リモート送信にはサーバー公開鍵の設定が必要です。");
            }

            return new HybridPayloadProtector(Convert.FromBase64String(options.ServerPublicKeyBase64), options.KeyId);
        });
        Services.TryAddSingleton<IEnvelopeFactory, EnvelopeFactory>();
        Services.TryAddSingleton<ITelemetryTransport, HttpTelemetryTransport>();
        Services.TryAddSingleton<ILocalSignalStore, FileLocalSignalStore>();
        Services.TryAddSingleton<TransportTelemetrySink>();
        Services.TryAddSingleton<LocalStoreReplayer>();
    }

    private void RegisterLocalFile()
    {
        if (_useLocalFile)
        {
            Services.TryAddSingleton<FileTelemetrySink>();
        }
    }

    private void RegisterSink()
    {
        Services.TryAddSingleton<ITelemetrySink>(sp => BuildSink(sp));
    }

    private ITelemetrySink BuildSink(IServiceProvider sp)
    {
        var sinks = new List<ITelemetrySink>();
        if (_useRemote)
        {
            var transportSink = sp.GetRequiredService<TransportTelemetrySink>();
            var store = sp.GetRequiredService<ILocalSignalStore>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FailoverTelemetrySink>>();
            sinks.Add(new FailoverTelemetrySink(transportSink, store, logger));
        }

        if (_useLocalFile)
        {
            sinks.Add(sp.GetRequiredService<FileTelemetrySink>());
        }

        return sinks.Count switch
        {
            0 => NullTelemetrySink.Instance,
            1 => sinks[0],
            _ => new CompositeTelemetrySink(sinks)
        };
    }

    private void RegisterDispatcher()
    {
        Services.AddSingleton(sp => new TelemetryDispatcher(
            sp.GetRequiredService<ISignalExporter>(),
            sp.GetService<LocalStoreReplayer>(),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<DispatchOptions>>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TelemetryDispatcher>>()));
        Services.AddHostedService(sp => sp.GetRequiredService<TelemetryDispatcher>());
    }
}
