using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxyHostHeaderRewriter" /> verifying that a request's
///     <c>Host</c> header is rewritten to point at the backend host (with the port omitted
///     when default), the rest of the request semantics are preserved, and a Host header is
///     added when missing.
/// </summary>
public sealed class ReverseProxyHostHeaderRewriterTests
{
    /// <summary>
    ///     Default HTTP port (80) is omitted from the rewritten Host header value.
    /// </summary>
    [Test]
    public async Task Rewrite_DefaultHttpPort_OmitsPortFromHostHeader()
    {
        var request = BuildRequest(originalHost: "example.com");

        var rewritten = ReverseProxyHostHeaderRewriter.Rewrite(request, "backend.local", 80);

        await Assert.That(rewritten.Headers.Get("Host")).IsEqualTo("backend.local");
    }

    /// <summary>
    ///     Default HTTPS port (443) is also omitted.
    /// </summary>
    [Test]
    public async Task Rewrite_DefaultHttpsPort_OmitsPortFromHostHeader()
    {
        var request = BuildRequest(originalHost: "example.com");

        var rewritten = ReverseProxyHostHeaderRewriter.Rewrite(request, "backend.local", 443);

        await Assert.That(rewritten.Headers.Get("Host")).IsEqualTo("backend.local");
    }

    /// <summary>
    ///     Non-default port is included in the Host header value.
    /// </summary>
    [Test]
    public async Task Rewrite_NonDefaultPort_IncludesPortInHostHeader()
    {
        var request = BuildRequest(originalHost: "example.com");

        var rewritten = ReverseProxyHostHeaderRewriter.Rewrite(request, "backend.local", 9090);

        await Assert.That(rewritten.Headers.Get("Host")).IsEqualTo("backend.local:9090");
    }

    /// <summary>
    ///     An existing Host header is replaced with the backend value, regardless of case.
    /// </summary>
    [Test]
    public async Task Rewrite_MixedCaseHostHeader_ReplacesHostHeader()
    {
        var headers = HeaderCollection.Empty
            .Add("hOsT", "example.com")
            .Add("Accept", "text/html");
        var request = BuildRequestWithHeaders(headers);

        var rewritten = ReverseProxyHostHeaderRewriter.Rewrite(request, "backend.local", 8080);

        await Assert.That(rewritten.Headers.Get("Host")).IsEqualTo("backend.local:8080");
        await Assert.That(rewritten.Headers.Get("Accept")).IsEqualTo("text/html");
    }

    /// <summary>
    ///     When the request has no Host header (HTTP/1.0 / absolute URI), one is added.
    /// </summary>
    [Test]
    public async Task Rewrite_MissingHostHeader_AddsHostHeader()
    {
        var headers = HeaderCollection.Empty.Add("Accept", "text/html");
        var request = BuildRequestWithHeaders(headers);

        var rewritten = ReverseProxyHostHeaderRewriter.Rewrite(request, "backend.local", 9090);

        await Assert.That(rewritten.Headers.Get("Host")).IsEqualTo("backend.local:9090");
    }

    /// <summary>
    ///     Method, URI, version, and body are preserved exactly.
    /// </summary>
    [Test]
    public async Task Rewrite_AnyRequest_PreservesNonHostState()
    {
        var body = new byte[] { 1, 2, 3 };
        var headers = HeaderCollection.Empty.Add("Host", "example.com").Add("Content-Length", "3");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body,
            Headers = headers,
            Method = "POST",
            RequestUri = new Uri("/api/users?id=1", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var rewritten = ReverseProxyHostHeaderRewriter.Rewrite(request, "backend.local", 80);

        await Assert.That(rewritten.Method).IsEqualTo("POST");
        await Assert.That(rewritten.RequestUri).IsEqualTo(new Uri("/api/users?id=1", UriKind.Relative));
        await Assert.That(rewritten.Version).IsEqualTo("HTTP/1.1");
        await Assert.That(rewritten.Body).IsEqualTo(body);
        await Assert.That(rewritten.Headers.Get("Content-Length")).IsEqualTo("3");
    }

    /// <summary>
    ///     If the request contains multiple Host headers (broken client), only one Host
    ///     header is emitted in the rewrite.
    /// </summary>
    [Test]
    public async Task Rewrite_DuplicateHostHeaders_EmitsSingleHostHeader()
    {
        var headers = HeaderCollection.Empty
            .Add("Host", "a.example.com")
            .Add("Host", "b.example.com")
            .Add("Accept", "application/json");
        var request = BuildRequestWithHeaders(headers);

        var rewritten = ReverseProxyHostHeaderRewriter.Rewrite(request, "backend.local", 8080);

        var hostHeaderValues = rewritten.Headers.GetAll("Host");
        await Assert.That(hostHeaderValues.Length).IsEqualTo(1);
        await Assert.That(hostHeaderValues[0]).IsEqualTo("backend.local:8080");
    }

    private static HypertextTransferProtocolRequestData BuildRequest(string originalHost)
    {
        var headers = HeaderCollection.Empty.Add("Host", originalHost);
        return BuildRequestWithHeaders(headers);
    }

    private static HypertextTransferProtocolRequestData BuildRequestWithHeaders(HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("/", UriKind.Relative),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
