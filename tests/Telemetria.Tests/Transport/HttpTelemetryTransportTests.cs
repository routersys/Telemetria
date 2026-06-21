using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Telemetria.Transport;
using Telemetria.Tests.Support;
using Xunit;

namespace Telemetria.Tests.Transport;

public sealed class HttpTelemetryTransportTests
{
    private static ProtectedEnvelope Envelope()
        => new()
        {
            AnonymousId = "anon-1",
            OneTimePassword = "123456",
            CreatedAt = DateTimeOffset.UnixEpoch,
            Payload = new ProtectedPayload
            {
                KeyId = "key-1",
                EphemeralPublicKey = [1, 2, 3],
                Nonce = [4, 5, 6],
                CipherText = [7, 8, 9],
                Tag = [10, 11, 12]
            }
        };

    private static (HttpTelemetryTransport Transport, StubHttpMessageHandler Handler) Create(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        TransportOptions? options = null)
    {
        var handler = new StubHttpMessageHandler(responder);
        var factory = new SingleClientFactory(handler);
        var monitor = new StaticOptionsMonitor<TransportOptions>(options ?? new TransportOptions { Endpoint = new Uri("https://example.invalid/ingest") });
        return (new HttpTelemetryTransport(factory, monitor, NullLogger<HttpTelemetryTransport>.Instance), handler);
    }

    [Fact]
    public async Task SendAsync_SuccessStatus_ReturnsTrue()
    {
        var (transport, _) = Create(StatusResponses.WithStatus(HttpStatusCode.Accepted));
        Assert.True(await transport.SendAsync(Envelope()));
    }

    [Fact]
    public async Task SendAsync_ServerError_ReturnsFalse()
    {
        var (transport, _) = Create(StatusResponses.WithStatus(HttpStatusCode.InternalServerError));
        Assert.False(await transport.SendAsync(Envelope()));
    }

    [Fact]
    public async Task SendAsync_SetsOtpAndAnonymousHeaders()
    {
        var (transport, handler) = Create(StatusResponses.WithStatus(HttpStatusCode.OK));
        await transport.SendAsync(Envelope());

        var request = Assert.Single(handler.Requests);
        Assert.Equal("123456", request.Headers.GetValues("X-Telemetria-Otp").Single());
        Assert.Equal("anon-1", request.Headers.GetValues("X-Telemetria-Anonymous-Id").Single());
    }

    [Fact]
    public async Task SendAsync_PostsJsonBodyContainingKeyId()
    {
        var (transport, handler) = Create(StatusResponses.WithStatus(HttpStatusCode.OK));
        await transport.SendAsync(Envelope());

        var body = System.Text.Encoding.UTF8.GetString(Assert.Single(handler.Bodies));
        Assert.Contains("key-1", body);
        Assert.Contains("anon-1", body);
    }

    [Fact]
    public async Task SendAsync_NoEndpoint_Throws()
    {
        var (transport, _) = Create(StatusResponses.WithStatus(HttpStatusCode.OK), new TransportOptions { Endpoint = null });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await transport.SendAsync(Envelope()));
    }

    [Fact]
    public async Task SendAsync_HttpRequestException_ReturnsFalse()
    {
        var (transport, _) = Create(_ => throw new HttpRequestException("connection refused"));
        Assert.False(await transport.SendAsync(Envelope()));
    }

    [Fact]
    public async Task SendAsync_WithRequestSignature_SendsSignatureHeader()
    {
        var (transport, handler) = Create(StatusResponses.WithStatus(HttpStatusCode.OK));
        var envelope = Envelope() with { RequestSignature = "sig-abc" };
        await transport.SendAsync(envelope);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("sig-abc", request.Headers.GetValues("X-Telemetria-Signature").Single());
    }

    [Fact]
    public async Task SendAsync_WithoutRequestSignature_OmitsSignatureHeader()
    {
        var (transport, handler) = Create(StatusResponses.WithStatus(HttpStatusCode.OK));
        await transport.SendAsync(Envelope());

        var request = Assert.Single(handler.Requests);
        Assert.False(request.Headers.Contains("X-Telemetria-Signature"));
    }

    [Fact]
    public async Task SendAsync_CustomSignatureHeader_UsesConfiguredName()
    {
        var options = new TransportOptions
        {
            Endpoint = new Uri("https://example.invalid/ingest"),
            SignatureHeader = "X-Custom-Sig"
        };
        var (transport, handler) = Create(StatusResponses.WithStatus(HttpStatusCode.OK), options);
        var envelope = Envelope() with { RequestSignature = "custom-sig" };
        await transport.SendAsync(envelope);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("custom-sig", request.Headers.GetValues("X-Custom-Sig").Single());
    }
}
