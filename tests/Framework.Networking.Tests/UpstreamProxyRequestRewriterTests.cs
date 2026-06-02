using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="UpstreamProxyRequestRewriter" />.
/// </summary>
public sealed class UpstreamProxyRequestRewriterTests
{
    /// <summary>
    ///     Verifies that a relative request URI is rewritten to an absolute http:// URI using
    ///     the Host header.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_RelativeUri_RewritesToAbsoluteForm()
    {
        var originalBytes = Encoding.ASCII.GetBytes("GET /api/users HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET http://example.com/api/users HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that the rewritten output preserves the rest of the header bytes after the
    ///     request line.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_MultiHeaderRequest_PreservesRemainingHeaders()
    {
        var originalBytes = Encoding.ASCII.GetBytes("GET /api/users HTTP/1.1\r\nHost: example.com\r\nAccept: */*\r\n\r\n");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Host: example.com");
        await Assert.That(asString).Contains("Accept: */*");
    }

    /// <summary>
    ///     Verifies that an absolute URI request preserves the URI as the absolute form.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_AbsoluteUri_PreservesAbsoluteForm()
    {
        var originalBytes = Encoding.ASCII.GetBytes("GET http://example.com/api HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("http://example.com/api"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET http://example.com/api HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that header bytes lacking a CRLF terminator are returned unchanged.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_NoCarriageReturnLineFeed_ReturnsOriginalBytes()
    {
        var originalBytes = Encoding.ASCII.GetBytes("GET /api HTTP/1.1");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request);

        await Assert.That(rewritten.Length).IsEqualTo(originalBytes.Length);
    }

    /// <summary>
    ///     Verifies that a relative URI without a Host header uses "unknown" as the host.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_RelativeUriWithoutHostHeader_UsesUnknownHost()
    {
        var originalBytes = Encoding.ASCII.GetBytes("GET /api HTTP/1.1\r\n\r\n");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("/api", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("http://unknown");
    }

    /// <summary>
    ///     Verifies that the Proxy-Authorization header is injected directly after the request line
    ///     when supplied.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_WithProxyAuthorization_InjectsHeader()
    {
        var originalBytes = Encoding.ASCII.GetBytes("GET /api HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request, "Basic YWxpY2U6c2VjcmV0");
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Proxy-Authorization: Basic YWxpY2U6c2VjcmV0");
        await Assert.That(asString).Contains("Host: example.com");
    }

    /// <summary>
    ///     Verifies that any pre-existing Proxy-Authorization header from the client is removed when
    ///     credentials are supplied, so the upstream sees only the configured credentials.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_WithProxyAuthorization_StripsPreExistingClientHeader()
    {
        var originalBytes = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nProxy-Authorization: Basic clientToken==\r\n\r\n");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request, "Basic configured");
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("clientToken==");
        await Assert.That(asString).Contains("Proxy-Authorization: Basic configured");
    }

    /// <summary>
    ///     Verifies that any pre-existing Proxy-Authorization header from the client is stripped
    ///     even when no replacement credentials are supplied. Issue #39 — the header is
    ///     hop-by-hop and must never leak to the upstream proxy (RFC 9110 §11.7.1). Exercises
    ///     the two-argument overload.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_NoProxyAuthorization_StripsClientHeader()
    {
        var originalBytes = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nProxy-Authorization: Basic clientToken==\r\n\r\n");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Proxy-Authorization");
        await Assert.That(asString).DoesNotContain("clientToken==");
    }

    /// <summary>
    ///     Verifies that the three-argument overload strips a client Proxy-Authorization header
    ///     when invoked with an explicit null. This is the exact call shape used by
    ///     <see cref="HypertextTransferProtocolForwarder" /> when no upstream credentials are
    ///     configured (issue #39).
    /// </summary>
    [Test]
    public async Task RewriteHeaders_NullProxyAuthorization_StripsClientHeader()
    {
        var originalBytes = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nProxy-Authorization: Basic clientToken==\r\n\r\n");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request, proxyAuthorization: null);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Proxy-Authorization");
        await Assert.That(asString).DoesNotContain("clientToken==");
    }

    /// <summary>
    ///     Verifies that a Proxy-Authorization header with leading whitespace before the colon
    ///     (lenient parsing) is still stripped. Without trimming the header name before the
    ///     strip lookup, malformed-but-tolerated headers would bypass the privacy fix from
    ///     issue #39.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_HeaderNameHasLeadingTrailingWhitespace_StillStrips()
    {
        var originalBytes = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\n Proxy-Authorization : Basic clientToken==\r\n\r\n");
        var request = BuildRelativeRequest();

        var rewritten = UpstreamProxyRequestRewriter.RewriteHeaders(originalBytes, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("clientToken==");
        await Assert.That(asString).DoesNotContain("Proxy-Authorization");
    }

    private static HypertextTransferProtocolRequestData BuildRelativeRequest()
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("/api/users", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
