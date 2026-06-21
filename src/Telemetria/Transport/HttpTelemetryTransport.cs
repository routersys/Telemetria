using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telemetria.Serialization;

namespace Telemetria.Transport;

/// <summary>
/// HTTP を介して保護済みエンベロープを送信する通信手段です。
/// </summary>
public sealed class HttpTelemetryTransport : ITelemetryTransport
{
    /// <summary>このクラスが用いる <see cref="HttpClient"/> の論理名です。</summary>
    public const string HttpClientName = "Telemetria";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<TransportOptions> _options;
    private readonly ILogger<HttpTelemetryTransport> _logger;

    /// <summary>依存関係を指定して初期化します。</summary>
    public HttpTelemetryTransport(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<TransportOptions> options,
        ILogger<HttpTelemetryTransport> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> SendAsync(ProtectedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var options = _options.CurrentValue;
        if (options.Endpoint is null)
        {
            throw new InvalidOperationException("送信先エンドポイントが設定されていません。");
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, TelemetriaJsonContext.Default.ProtectedEnvelope);

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint)
        {
            Content = new ByteArrayContent(body)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        request.Headers.TryAddWithoutValidation(options.OneTimePasswordHeader, envelope.OneTimePassword);
        request.Headers.TryAddWithoutValidation(options.AnonymousIdHeader, envelope.AnonymousId);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(options.Timeout);

        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await client.SendAsync(request, timeoutSource.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("送信先が異常応答を返しました: {StatusCode}", (int)response.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "送信先への接続に失敗しました。");
            return false;
        }
    }
}
