using Proxyfan.Framework.Extensibility;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolPluginUpdateFeed" />.
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolPluginUpdateFeedTests
{
    private const string ManifestSigningPrivateKeyPkcs8Base64 = "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgTQTs+TsFeht8UnXwDud1t16oDXCM/F75LAB7Wskxy8ehRANCAASz0xuLKTTtX0D67XWAn/7rX5vK+QkFPLS7hnEF6BdJb7mO+ule2ZkwPZFVRzHKH7U+VozJ+bejgI2ZhvE6aszd";

    /// <summary>
    ///     An empty manifest URL short-circuits without an HTTP call.
    /// </summary>
    [Test]
    public async Task FetchAsync_EmptyUrl_ReturnsNull()
    {
        using var handler = new StubHttpMessageHandler("""{ "plugins": [] }""");
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider(string.Empty);
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
        await Assert.That(handler.RequestCount).IsEqualTo(0);
    }

    /// <summary>
    ///     HTTP manifest URLs are rejected before a request is sent.
    /// </summary>
    [Test]
    public async Task FetchAsync_ManifestUrlIsHttp_RejectsRequest()
    {
        using var handler = new StubHttpMessageHandler("""{ "plugins": [] }""");
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("http://plugins.proxyfan.dev/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
        await Assert.That(handler.RequestCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Hosts outside the trusted allow-list are rejected before a request is sent.
    /// </summary>
    [Test]
    public async Task FetchAsync_ManifestHostNotTrusted_RejectsRequest()
    {
        using var handler = new StubHttpMessageHandler("""{ "plugins": [] }""");
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("https://attacker.example/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
        await Assert.That(handler.RequestCount).IsEqualTo(0);
    }

    /// <summary>
    ///     A 200 OK response with a valid signed manifest body returns a parsed manifest.
    /// </summary>
    [Test]
    public async Task FetchAsync_HappyPath_ReturnsManifest()
    {
        var pluginsJson = """
            [
              { "id": "com.x", "latestVersion": "1.0.0", "downloadUrl": "https://example.invalid/com.x.zip", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "minApiVersion": "0.0" }
            ]
            """;
        var json = BuildSignedManifest(pluginsJson);
        using var handler = new StubHttpMessageHandler(json);
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("https://plugins.proxyfan.dev/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
        await Assert.That(handler.RequestCount).IsEqualTo(1);
    }

    /// <summary>
    ///     A 500 response returns null instead of throwing.
    /// </summary>
    [Test]
    public async Task FetchAsync_ServerError_ReturnsNull()
    {
        using var handler = new StubHttpMessageHandler("internal error", HttpStatusCode.InternalServerError);
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("https://plugins.proxyfan.dev/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A response with malformed JSON returns null.
    /// </summary>
    [Test]
    public async Task FetchAsync_MalformedBody_ReturnsNull()
    {
        using var handler = new StubHttpMessageHandler("not json");
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("https://plugins.proxyfan.dev/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A connection-level HTTP request error returns null instead of throwing.
    /// </summary>
    [Test]
    public async Task FetchAsync_HttpRequestException_ReturnsNull()
    {
        using var handler = new ThrowingHttpMessageHandler();
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("https://plugins.proxyfan.dev/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A response whose advertised Content-Length exceeds the manifest budget returns null
    ///     without buffering the body.
    /// </summary>
    [Test]
    public async Task FetchAsync_AdvertisedContentLengthExceedsBudget_ReturnsNull()
    {
        var oversized = HypertextTransferProtocolPluginUpdateFeed.ManifestSizeLimitInBytes + 1;
        using var handler = new StubHttpMessageHandler("{}", HttpStatusCode.OK, oversized);
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("https://plugins.proxyfan.dev/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A response that streams more bytes than the manifest budget (regardless of headers)
    ///     returns null and stops reading.
    /// </summary>
    [Test]
    public async Task FetchAsync_StreamedBodyExceedsBudget_ReturnsNull()
    {
        var oversized = new string('x', HypertextTransferProtocolPluginUpdateFeed.ManifestSizeLimitInBytes + 1024);
        using var handler = new StubHttpMessageHandler(oversized);
        using var client = new HttpClient(handler);
        var provider = new PluginUpdateManifestUrlProvider("https://plugins.proxyfan.dev/manifest.json");
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    private static string BuildSignedManifest(string pluginsJson)
    {
        var signature = Sign(pluginsJson);
        return $$"""
            {
              "signature": "{{signature}}",
              "plugins": {{pluginsJson}}
            }
            """;
    }

    private static string Sign(string pluginsJson)
    {
        var privateKeyBytes = Convert.FromBase64String(ManifestSigningPrivateKeyPkcs8Base64);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
        using var document = JsonDocument.Parse(pluginsJson);
        var signedPayload = document.RootElement.GetRawText();
        var payload = Encoding.UTF8.GetBytes(signedPayload);
        var signature = ecdsa.SignData(payload, HashAlgorithmName.SHA256);
        return Convert.ToBase64String(signature);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;
        private readonly int? _advertisedContentLength;

        public StubHttpMessageHandler(string responseBody)
            : this(responseBody, HttpStatusCode.OK, null)
        {
        }

        public StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode)
            : this(responseBody, statusCode, null)
        {
        }

        public StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode, int? advertisedContentLength)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
            _advertisedContentLength = advertisedContentLength;
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = new HttpResponseMessage(_statusCode);
            var content = new StringContent(_responseBody, Encoding.UTF8, "application/json");
            if (_advertisedContentLength is { } contentLength)
            {
                content.Headers.ContentLength = contentLength;
            }

            response.Content = content;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("connection refused");
        }
    }
}
