using Proxyfan.Framework.Extensibility;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolPluginUpdateFeed" />.
/// </summary>
[NotInParallel]
public sealed class HypertextTransferProtocolPluginUpdateFeedTests
{
    /// <summary>
    ///     An empty manifest URL short-circuits without an HTTP call.
    /// </summary>
    [Test]
    public async Task FetchAsync_EmptyUrl_ReturnsNull()
    {
        using var client = new HttpClient();
        var provider = new PluginUpdateManifestUrlProvider(string.Empty);
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A 200 OK response with a valid manifest body returns a parsed manifest.
    /// </summary>
    [Test]
    public async Task FetchAsync_HappyPath_ReturnsManifest()
    {
        var json = """{ "plugins": [ { "id": "com.x", "latestVersion": "1.0.0" } ] }""";
        using var server = LoopbackHttpServer.Start(200, json);
        using var client = new HttpClient();
        var provider = new PluginUpdateManifestUrlProvider(server.Url);
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNotNull();
        await Assert.That(manifest!.Plugins).Count().IsEqualTo(1);
    }

    /// <summary>
    ///     A 500 response returns null instead of throwing.
    /// </summary>
    [Test]
    public async Task FetchAsync_ServerError_ReturnsNull()
    {
        using var server = LoopbackHttpServer.Start(500, "internal error");
        using var client = new HttpClient();
        var provider = new PluginUpdateManifestUrlProvider(server.Url);
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
        using var server = LoopbackHttpServer.Start(200, "not json");
        using var client = new HttpClient();
        var provider = new PluginUpdateManifestUrlProvider(server.Url);
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    /// <summary>
    ///     A connection refused (unreachable URL) returns null instead of throwing.
    /// </summary>
    [Test]
    public async Task FetchAsync_ConnectionRefused_ReturnsNull()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var provider = new PluginUpdateManifestUrlProvider("http://127.0.0.1:1/");
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
        using var server = LoopbackHttpServer.Start(200, "{}", advertisedContentLength: oversized);
        using var client = new HttpClient();
        var provider = new PluginUpdateManifestUrlProvider(server.Url);
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
        using var server = LoopbackHttpServer.Start(200, oversized, omitContentLength: true);
        using var client = new HttpClient();
        var provider = new PluginUpdateManifestUrlProvider(server.Url);
        var feed = new HypertextTransferProtocolPluginUpdateFeed(client, provider);

        var manifest = await feed.FetchAsync(CancellationToken.None);

        await Assert.That(manifest).IsNull();
    }

    private sealed class LoopbackHttpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cancellation;

        public string Url { get; }

        private LoopbackHttpServer(TcpListener listener, string url)
        {
            _listener = listener;
            Url = url;
            _cancellation = new CancellationTokenSource();
        }

        public static LoopbackHttpServer Start(int statusCode, string body, int? advertisedContentLength = null, bool omitContentLength = false)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var url = $"http://127.0.0.1:{port}/manifest.json";
            var server = new LoopbackHttpServer(listener, url);
            _ = Task.Run(() => server.AcceptOneAsync(statusCode, body, advertisedContentLength, omitContentLength));
            return server;
        }

        public void Dispose()
        {
            try { _cancellation.Cancel(); } catch (Exception ex) { _ = ex; }
            try { _listener.Stop(); } catch (Exception ex) { _ = ex; }
            _cancellation.Dispose();
        }

        private async Task AcceptOneAsync(int statusCode, string body, int? advertisedContentLength, bool omitContentLength)
        {
            try
            {
                using var clientSocket = await _listener.AcceptTcpClientAsync(_cancellation.Token).ConfigureAwait(false);
                using var stream = clientSocket.GetStream();
                var requestBuffer = new byte[8192];
                _ = await stream.ReadAsync(requestBuffer, _cancellation.Token).ConfigureAwait(false);
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                var statusText = statusCode == 200 ? "OK" : "Internal Server Error";
                var headerBuilder = new StringBuilder();
                headerBuilder.Append(System.Globalization.CultureInfo.InvariantCulture, $"HTTP/1.1 {statusCode} {statusText}\r\n");
                headerBuilder.Append("Content-Type: application/json\r\n");
                if (omitContentLength)
                {
                    // Force chunked-style "read until close" so the client can't pre-validate length.
                }
                else
                {
                    var contentLength = advertisedContentLength ?? bodyBytes.Length;
                    headerBuilder.Append(System.Globalization.CultureInfo.InvariantCulture, $"Content-Length: {contentLength}\r\n");
                }
                headerBuilder.Append("Connection: close\r\n\r\n");
                var headerBytes = Encoding.ASCII.GetBytes(headerBuilder.ToString());
                await stream.WriteAsync(headerBytes, _cancellation.Token).ConfigureAwait(false);
                await stream.WriteAsync(bodyBytes, _cancellation.Token).ConfigureAwait(false);
                await stream.FlushAsync(_cancellation.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }
}
