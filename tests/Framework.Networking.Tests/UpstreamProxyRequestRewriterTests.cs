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
