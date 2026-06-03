using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="UpgradeRequestRewriter" /> verifying that the WebSocket-aware
///     request rewriter preserves <c>Connection</c> and <c>Upgrade</c> while still stripping
///     <c>Proxy-*</c> headers and appending the <c>Via</c> token.
/// </summary>
public sealed class UpgradeRequestRewriterTests
{
    /// <summary>Verifies that the <c>Connection: upgrade</c> header passes through unchanged.</summary>
    [Test]
    public async Task RewriteHeaders_UpgradeRequest_PreservesConnectionUpgrade()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nConnection: upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Key: abc\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Connection: upgrade");
        await Assert.That(asString).Contains("Upgrade: websocket");
        await Assert.That(asString).Contains("Sec-WebSocket-Key: abc");
    }

    /// <summary>Verifies that the request line is rewritten to origin-form even for upgrades.</summary>
    [Test]
    public async Task RewriteHeaders_AbsoluteUriUpgrade_RewritesToOriginForm()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET http://example.com/chat HTTP/1.1\r\nHost: example.com\r\nConnection: upgrade\r\nUpgrade: websocket\r\n\r\n");
        var request = BuildAbsoluteRequest("GET", "http://example.com/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET /chat HTTP/1.1");
    }

    /// <summary>Verifies that Proxy-Authorization is stripped from upgrade requests.</summary>
    [Test]
    public async Task RewriteHeaders_ProxyAuthorization_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nProxy-Authorization: Basic foo\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Proxy-Authorization");
    }

    /// <summary>Verifies that Proxy-Connection is stripped from upgrade requests.</summary>
    [Test]
    public async Task RewriteHeaders_ProxyConnection_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nProxy-Connection: keep-alive\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Proxy-Connection");
    }

    /// <summary>Verifies that a Via header is appended when none exists in the request.</summary>
    [Test]
    public async Task RewriteHeaders_NoVia_AppendsViaHeader()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nConnection: upgrade\r\nUpgrade: websocket\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Via: 1.1 proxyfan");
    }

    /// <summary>Verifies that an existing Via chain is extended rather than replaced.</summary>
    [Test]
    public async Task RewriteHeaders_ExistingVia_AppendsToken()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nVia: 1.1 edge\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Via: 1.1 edge, 1.1 proxyfan");
    }

    /// <summary>Verifies that bytes with no CRLF in the first line are returned unchanged.</summary>
    [Test]
    public async Task RewriteHeaders_NoLineTerminator_ReturnsOriginal()
    {
        var original = Encoding.ASCII.GetBytes("GET /chat HTTP/1.1");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);

        await Assert.That(Encoding.ASCII.GetString(rewritten)).IsEqualTo(Encoding.ASCII.GetString(original));
    }

    /// <summary>Verifies that the rewritten output terminates with the blank-line header terminator.</summary>
    [Test]
    public async Task RewriteHeaders_AnyInput_EndsWithCrlfCrlf()
    {
        var original = Encoding.ASCII.GetBytes("GET /chat HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString.EndsWith("\r\n\r\n", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>Verifies that a malformed header line is forwarded unchanged.</summary>
    [Test]
    public async Task RewriteHeaders_MalformedHeaderLine_ForwardedVerbatim()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nMalformedNoColon\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("MalformedNoColon");
    }

    /// <summary>Verifies that a header named by Connection is stripped before forwarding.</summary>
    [Test]
    public async Task RewriteHeaders_ConnectionListedHeader_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nConnection: upgrade, X-Hop\r\nUpgrade: websocket\r\nX-Hop: secret\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("X-Hop: secret");
        await Assert.That(asString).Contains("Connection: upgrade, X-Hop");
        await Assert.That(asString).Contains("Upgrade: websocket");
    }

    /// <summary>Verifies that standard hop-by-hop headers are stripped from upgrade requests.</summary>
    [Test]
    public async Task RewriteHeaders_HopByHopHeaders_AreStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: example.com\r\nConnection: upgrade\r\nUpgrade: websocket\r\nKeep-Alive: timeout=5\r\nTE: trailers\r\nTrailer: Expires\r\nTransfer-Encoding: chunked\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/chat");

        var rewritten = UpgradeRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Keep-Alive");
        await Assert.That(asString).DoesNotContain("TE:");
        await Assert.That(asString).DoesNotContain("Trailer:");
        await Assert.That(asString).DoesNotContain("Transfer-Encoding");
        await Assert.That(asString).Contains("Connection: upgrade");
        await Assert.That(asString).Contains("Upgrade: websocket");
    }

    private static HypertextTransferProtocolRequestData BuildAbsoluteRequest(string method, string absoluteUri)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = method,
            RequestUri = new Uri(absoluteUri, UriKind.Absolute),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolRequestData BuildRelativeRequest(string method, string path)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = method,
            RequestUri = new Uri(path, UriKind.Relative),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
