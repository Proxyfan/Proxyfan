using Proxyfan.Domain.Traffic;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="OriginRequestRewriter" /> verifying RFC 7230 hop-by-hop header
///     stripping, origin-form request line rewriting, and <c>Via</c> header injection.
/// </summary>
public sealed class OriginRequestRewriterTests
{
    /// <summary>
    ///     Verifies that an absolute-URI request line is rewritten to origin-form (path-only).
    /// </summary>
    [Test]
    public async Task RewriteHeaders_AbsoluteUri_RewritesToOriginForm()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET http://example.com/api/users?id=1 HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildAbsoluteRequest("GET", "http://example.com/api/users?id=1");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET /api/users?id=1 HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that an absolute URI without a path becomes <c>/</c>.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_AbsoluteUriNoPath_RewritesToSlash()
    {
        var original = Encoding.ASCII.GetBytes("GET http://example.com HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildAbsoluteRequest("GET", "http://example.com");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET / HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that an existing origin-form request line is preserved.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_RelativeUri_PreservesOriginForm()
    {
        var original = Encoding.ASCII.GetBytes("GET /api/users HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api/users");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET /api/users HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that the asterisk-form request target for <c>OPTIONS *</c> is preserved.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_OptionsAsterisk_PreservesAsteriskForm()
    {
        var original = Encoding.ASCII.GetBytes("OPTIONS * HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest("OPTIONS", "*");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("OPTIONS * HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that a fragment is stripped from the rewritten request target.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_RelativeUriWithFragment_StripsFragment()
    {
        var original = Encoding.ASCII.GetBytes("GET /index#section HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/index#section");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET /index HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that <c>Proxy-Authorization</c> is never forwarded to the origin.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_ProxyAuthorizationPresent_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nProxy-Authorization: Basic dXNlcjpwYXNz\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Proxy-Authorization");
    }

    /// <summary>
    ///     Verifies that <c>Proxy-Connection</c> is stripped from forwarded requests.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_ProxyConnectionPresent_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nProxy-Connection: keep-alive\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Proxy-Connection");
    }

    /// <summary>
    ///     Verifies that the <c>Connection</c> header itself is stripped from forwarded requests.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_ConnectionHeader_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nConnection: close\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Connection: close");
    }

    /// <summary>
    ///     Verifies that headers listed in the <c>Connection</c> header value are also stripped.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_ConnectionListedHeaders_AreStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nConnection: X-Foo, X-Bar\r\nX-Foo: 1\r\nX-Bar: 2\r\nX-Baz: keep\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("X-Foo: 1");
        await Assert.That(asString).DoesNotContain("X-Bar: 2");
        await Assert.That(asString).Contains("X-Baz: keep");
    }

    /// <summary>
    ///     Verifies that header name matching is case-insensitive when stripping connection-scoped names.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_LowercaseConnectionListedName_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nconnection: x-trace\r\nX-Trace: abc\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("X-Trace: abc");
    }

    /// <summary>
    ///     Verifies that <c>Keep-Alive</c> is stripped regardless of whether it was listed in Connection.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_KeepAliveHeader_IsStripped()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nKeep-Alive: timeout=5\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).DoesNotContain("Keep-Alive");
    }

    /// <summary>
    ///     Verifies that a <c>Via: 1.1 proxyfan</c> header is appended when no <c>Via</c> exists.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_NoExistingVia_AppendsViaHeader()
    {
        var original = Encoding.ASCII.GetBytes("GET /api HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Via: 1.1 proxyfan");
    }

    /// <summary>
    ///     Verifies that an existing <c>Via</c> chain has the proxy's token appended, not replaced.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_ExistingVia_AppendsToken()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nVia: 1.1 edge-proxy\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Via: 1.1 edge-proxy, 1.1 proxyfan");
    }

    /// <summary>
    ///     Verifies that the rewritten output terminates with the blank-line header terminator.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_AnyInput_EndsWithCrlfCrlf()
    {
        var original = Encoding.ASCII.GetBytes("GET /api HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString.EndsWith("\r\n\r\n", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that other request headers (Accept, User-Agent) survive the rewrite.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_NonHopByHopHeaders_ArePreserved()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nAccept: */*\r\nUser-Agent: Mozilla/5.0\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("Host: example.com");
        await Assert.That(asString).Contains("Accept: */*");
        await Assert.That(asString).Contains("User-Agent: Mozilla/5.0");
    }

    /// <summary>
    ///     Verifies that bytes with no CRLF in the first line are returned unchanged.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_NoLineTerminator_ReturnsOriginal()
    {
        var original = Encoding.ASCII.GetBytes("GET /api HTTP/1.1");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);

        await Assert.That(Encoding.ASCII.GetString(rewritten)).IsEqualTo(Encoding.ASCII.GetString(original));
    }

    /// <summary>
    ///     Verifies that <c>Connection: keep-alive</c> tokens do not strip headers (only named extensions strip).
    /// </summary>
    [Test]
    public async Task RewriteHeaders_ConnectionKeepAliveOnly_DoesNotStripExtensions()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nConnection: keep-alive\r\nKeep-Alive: timeout=5\r\nX-Custom: value\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("X-Custom: value");
        await Assert.That(asString).DoesNotContain("Keep-Alive: timeout=5");
        await Assert.That(asString).DoesNotContain("Connection: keep-alive");
    }

    /// <summary>
    ///     Verifies that a relative URI consisting only of a fragment (e.g. <c>#foo</c>) becomes the
    ///     origin path <c>/</c> after stripping the fragment.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_RelativeUriFragmentOnly_RewritesToSlash()
    {
        var original = Encoding.ASCII.GetBytes("GET #section HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var request = BuildRelativeRequest("GET", "#section");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).StartsWith("GET / HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that a malformed header line with no colon (or a leading-colon line) is forwarded
    ///     verbatim — the rewriter must not crash on unparseable header lines.
    /// </summary>
    [Test]
    public async Task RewriteHeaders_MalformedHeaderLine_ForwardedVerbatim()
    {
        var original = Encoding.ASCII.GetBytes(
            "GET /api HTTP/1.1\r\nHost: example.com\r\nMalformedLineNoColon\r\n\r\n");
        var request = BuildRelativeRequest("GET", "/api");

        var rewritten = OriginRequestRewriter.RewriteHeaders(original, request);
        var asString = Encoding.ASCII.GetString(rewritten);

        await Assert.That(asString).Contains("MalformedLineNoColon");
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
