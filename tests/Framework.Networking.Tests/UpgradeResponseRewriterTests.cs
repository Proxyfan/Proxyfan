using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="UpgradeResponseRewriter" /> verifying that the WebSocket-aware
///     response rewriter preserves <c>Connection</c> and <c>Upgrade</c> while still stripping
///     <c>Keep-Alive</c> and <c>Proxy-*</c> headers and appending the <c>Via</c> token.
/// </summary>
public sealed class UpgradeResponseRewriterTests
{
    /// <summary>Verifies that the Connection and Upgrade headers are preserved.</summary>
    [Test]
    public async Task Rewrite_UpgradeResponse_PreservesConnectionUpgrade()
    {
        var headers = HeaderCollection.Empty
            .Add("Connection", "upgrade")
            .Add("Upgrade", "websocket")
            .Add("Sec-WebSocket-Accept", "xyz");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Connection")).IsEqualTo("upgrade");
        await Assert.That(rewritten.Headers.Get("Upgrade")).IsEqualTo("websocket");
        await Assert.That(rewritten.Headers.Get("Sec-WebSocket-Accept")).IsEqualTo("xyz");
    }

    /// <summary>Verifies that a Via header is appended when none exists.</summary>
    [Test]
    public async Task Rewrite_NoVia_AppendsProxyVia()
    {
        var response = CreateResponse(HeaderCollection.Empty);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Via")).IsEqualTo("1.1 proxyfan");
    }

    /// <summary>Verifies that an existing Via chain is extended.</summary>
    [Test]
    public async Task Rewrite_ExistingVia_ExtendsChain()
    {
        var headers = HeaderCollection.Empty.Add("Via", "1.0 upstream");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Via")).IsEqualTo("1.0 upstream, 1.1 proxyfan");
    }

    /// <summary>Verifies that Keep-Alive header is stripped.</summary>
    [Test]
    public async Task Rewrite_KeepAliveHeader_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Keep-Alive", "timeout=5");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Keep-Alive")).IsFalse();
    }

    /// <summary>Verifies that Proxy-Authenticate is stripped.</summary>
    [Test]
    public async Task Rewrite_ProxyAuthenticate_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Proxy-Authenticate", "Basic realm=\"x\"");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Proxy-Authenticate")).IsFalse();
    }

    /// <summary>Verifies that Proxy-Connection is stripped.</summary>
    [Test]
    public async Task Rewrite_ProxyConnection_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Proxy-Connection", "close");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Proxy-Connection")).IsFalse();
    }

    /// <summary>Verifies that status code, reason phrase, version, and body are preserved.</summary>
    [Test]
    public async Task Rewrite_StatusAndBody_ArePreserved()
    {
        var body = new byte[] { 9, 8, 7 };
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "Switching Protocols",
            StatusCode = 101,
            Version = "HTTP/1.1",
        });

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.StatusCode).IsEqualTo(101);
        await Assert.That(rewritten.ReasonPhrase).IsEqualTo("Switching Protocols");
        await Assert.That(rewritten.Version).IsEqualTo("HTTP/1.1");
        await Assert.That(rewritten.Body.ToArray()).IsEquivalentTo(body);
    }

    /// <summary>Verifies that the rewriter returns a new response instance.</summary>
    [Test]
    public async Task Rewrite_AnyResponse_ReturnsNewInstance()
    {
        var response = CreateResponse(HeaderCollection.Empty);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten).IsNotSameReferenceAs(response);
    }

    /// <summary>Verifies that headers listed in the Connection header are stripped.</summary>
    [Test]
    public async Task Rewrite_ConnectionListedHeader_IsStripped()
    {
        var headers = HeaderCollection.Empty
            .Add("Connection", "upgrade, x-upstream-only")
            .Add("Upgrade", "websocket")
            .Add("X-Upstream-Only", "secret");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("X-Upstream-Only")).IsFalse();
        await Assert.That(rewritten.Headers.Get("Upgrade")).IsEqualTo("websocket");
        await Assert.That(rewritten.Headers.Get("Connection")).IsEqualTo("upgrade, x-upstream-only");
    }

    /// <summary>Verifies that multiple Connection header values contribute tokens.</summary>
    [Test]
    public async Task Rewrite_MultipleConnectionHeaderValues_AllTokensStrip()
    {
        var headers = HeaderCollection.Empty
            .Add("Connection", "upgrade")
            .Add("Connection", "x-foo , x-bar")
            .Add("Upgrade", "websocket")
            .Add("X-Foo", "1")
            .Add("X-Bar", "2")
            .Add("X-Keep", "3");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("X-Foo")).IsFalse();
        await Assert.That(rewritten.Headers.HasHeader("X-Bar")).IsFalse();
        await Assert.That(rewritten.Headers.Get("X-Keep")).IsEqualTo("3");
    }

    /// <summary>Verifies that the Connection header itself is preserved even when self-listed.</summary>
    [Test]
    public async Task Rewrite_ConnectionListsItself_PreservesConnectionAndUpgrade()
    {
        var headers = HeaderCollection.Empty
            .Add("Connection", "Connection, Upgrade")
            .Add("Upgrade", "websocket");
        var response = CreateResponse(headers);

        var rewritten = UpgradeResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Connection")).IsEqualTo("Connection, Upgrade");
        await Assert.That(rewritten.Headers.Get("Upgrade")).IsEqualTo("websocket");
    }

    private static HypertextTransferProtocolResponseData CreateResponse(HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "Switching Protocols",
            StatusCode = 101,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
