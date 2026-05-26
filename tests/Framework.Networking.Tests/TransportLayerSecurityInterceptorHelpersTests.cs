using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportLayerSecurityInterceptorHelpers" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptorHelpersTests
{
    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.CreateClientTransportLayerSecurityOptions" />
    ///     sets the target host from the supplied <see cref="ConnectTarget" />.
    /// </summary>
    [Test]
    public async Task CreateClientTransportLayerSecurityOptions_WithTarget_SetsTargetHost()
    {
        var target = new ConnectTarget("api.example.com", 443);

        var options = TransportLayerSecurityInterceptorHelpers.CreateClientTransportLayerSecurityOptions(target);

        await Assert.That(options.TargetHost).IsEqualTo("api.example.com");
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.CreateServerTransportLayerSecurityOptions" />
    ///     populates the server certificate and disables client cert requirement.
    /// </summary>
    [Test]
    public async Task CreateServerTransportLayerSecurityOptions_WithLeafCertificate_SetsServerCertAndDisablesClientCert()
    {
        using var certificate = CreateSelfSignedCertificate("leaf.test");

        var options = TransportLayerSecurityInterceptorHelpers.CreateServerTransportLayerSecurityOptions(certificate);

        await Assert.That(options.ServerCertificate).IsNotNull();
        await Assert.That(options.ClientCertificateRequired).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.CreateTrafficFlow" /> uses
    ///     the connection's remote endpoint as the client endpoint.
    /// </summary>
    [Test]
    public async Task CreateTrafficFlow_WithRemoteEndPoint_UsesEndPointAsClient()
    {
        var connection = new StubFullDuplexProxyConnection();

        var flow = TransportLayerSecurityInterceptorHelpers.CreateTrafficFlow(connection);

        await Assert.That(flow.ClientEndPoint).Contains("127.0.0.1");
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.CreateTrafficFlow" /> falls
    ///     back to the literal "unknown" sentinel when the connection has no remote endpoint.
    /// </summary>
    [Test]
    public async Task CreateTrafficFlow_WithoutRemoteEndPoint_UsesUnknownSentinel()
    {
        var connection = new MissingEndPointConnection();

        var flow = TransportLayerSecurityInterceptorHelpers.CreateTrafficFlow(connection);

        await Assert.That(flow.ClientEndPoint).IsEqualTo("unknown");
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.HasConnectionCloseDirective" />
    ///     returns true for any case-insensitive "close" presence.
    /// </summary>
    [Test]
    public async Task HasConnectionCloseDirective_ContainsClose_ReturnsTrue()
    {
        var headers = HeaderCollection.Empty.Add("Connection", "Close");

        var result = TransportLayerSecurityInterceptorHelpers.HasConnectionCloseDirective(headers);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.HasConnectionCloseDirective" />
    ///     returns false when the header is absent.
    /// </summary>
    [Test]
    public async Task HasConnectionCloseDirective_HeaderMissing_ReturnsFalse()
    {
        var headers = HeaderCollection.Empty;

        var result = TransportLayerSecurityInterceptorHelpers.HasConnectionCloseDirective(headers);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.HasConnectionCloseDirective" />
    ///     returns false when the header is keep-alive.
    /// </summary>
    [Test]
    public async Task HasConnectionCloseDirective_KeepAlive_ReturnsFalse()
    {
        var headers = HeaderCollection.Empty.Add("Connection", "keep-alive");

        var result = TransportLayerSecurityInterceptorHelpers.HasConnectionCloseDirective(headers);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorHelpers.HasKeepAlive" /> returns
    ///     true for the canonical HTTP/1.1 + Content-Length + no close case.
    /// </summary>
    [Test]
    public async Task HasKeepAlive_CanonicalKeepAlive_ReturnsTrue()
    {
        var request = CreateRequest("HTTP/1.1");
        var responseHeaders = HeaderCollection.Empty.Add("Content-Length", "100");
        var response = CreateResponse(200, responseHeaders);

        var result = TransportLayerSecurityInterceptorHelpers.HasKeepAlive(request, response);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that an HTTP/1.0 request never keeps the connection alive.
    /// </summary>
    [Test]
    public async Task HasKeepAlive_HttpOnePointZero_ReturnsFalse()
    {
        var request = CreateRequest("HTTP/1.0");
        var responseHeaders = HeaderCollection.Empty.Add("Content-Length", "100");
        var response = CreateResponse(200, responseHeaders);

        var result = TransportLayerSecurityInterceptorHelpers.HasKeepAlive(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a response without Content-Length never keeps the connection alive.
    /// </summary>
    [Test]
    public async Task HasKeepAlive_NoContentLength_ReturnsFalse()
    {
        var request = CreateRequest("HTTP/1.1");
        var response = CreateResponse(200, HeaderCollection.Empty);

        var result = TransportLayerSecurityInterceptorHelpers.HasKeepAlive(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a request with Connection: close terminates keep-alive.
    /// </summary>
    [Test]
    public async Task HasKeepAlive_RequestConnectionClose_ReturnsFalse()
    {
        var request = CreateRequest("HTTP/1.1", HeaderCollection.Empty.Add("Connection", "close"));
        var responseHeaders = HeaderCollection.Empty.Add("Content-Length", "100");
        var response = CreateResponse(200, responseHeaders);

        var result = TransportLayerSecurityInterceptorHelpers.HasKeepAlive(request, response);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a response with Connection: close terminates keep-alive.
    /// </summary>
    [Test]
    public async Task HasKeepAlive_ResponseConnectionClose_ReturnsFalse()
    {
        var request = CreateRequest("HTTP/1.1");
        var responseHeaders = HeaderCollection.Empty
            .Add("Content-Length", "100")
            .Add("Connection", "close");
        var response = CreateResponse(200, responseHeaders);

        var result = TransportLayerSecurityInterceptorHelpers.HasKeepAlive(request, response);

        await Assert.That(result).IsFalse();
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string version)
    {
        return CreateRequest(version, HeaderCollection.Empty);
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string version, HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = version,
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData CreateResponse(int statusCode, HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string hostname)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={hostname}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var selfSigned = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var pfxBytes = selfSigned.Export(X509ContentType.Pfx);
        var loaded = X509CertificateLoader.LoadPkcs12(pfxBytes, string.Empty, X509KeyStorageFlags.Exportable);
        return loaded;
    }

    private sealed class MissingEndPointConnection : IProxyConnection
    {
        public EndPoint? RemoteEndPoint => null;

        public System.IO.Pipelines.IDuplexPipe Transport => throw new System.NotSupportedException();

        public System.Threading.Tasks.ValueTask DisposeAsync()
        {
            return System.Threading.Tasks.ValueTask.CompletedTask;
        }
    }
}
