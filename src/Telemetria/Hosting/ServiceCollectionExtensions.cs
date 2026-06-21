using Microsoft.Extensions.DependencyInjection;
using Telemetria.Hosting;

namespace Telemetria;

/// <summary>
/// テレメトリを依存性注入へ登録するための拡張メソッドを提供します。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>テレメトリの各種サービスを登録します。</summary>
    public static IServiceCollection AddTelemetria(this IServiceCollection services, Action<TelemetriaBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions();
        var builder = new TelemetriaBuilder(services);
        configure(builder);
        builder.Build();
        return services;
    }
}
