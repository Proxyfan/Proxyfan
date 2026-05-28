using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Networking;
using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace Proxyfan.Framework.Networking.Tests;

public sealed class ForwardedResponseRewriterTests
{
    [Test]
    public async Task Rewrite_NoVia_AppendsProxyVia()
    {
        var response = CreateResponse(HeaderCollection.Empty.Add("Content-Length", "5"));

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Via")).IsEqualTo("1.1 proxyfan");
    }

    [Test]
    public async Task Rewrite_ExistingViaSingleValue_ExtendsViaChain()
    {
        var headers = HeaderCollection.Empty.Add("Via", "1.0 upstream");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Via")).IsEqualTo("1.0 upstream, 1.1 proxyfan");
    }

    [Test]
    public async Task Rewrite_ExistingViaMultipleValues_JoinsThenAppendsProxy()
    {
        var headers = HeaderCollection.Empty
            .Add("Via", "1.0 alpha")
            .Add("Via", "1.1 beta");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Via")).IsEqualTo("1.0 alpha, 1.1 beta, 1.1 proxyfan");
    }

    [Test]
    public async Task Rewrite_ConnectionHeader_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Connection", "close");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Connection")).IsFalse();
    }

    [Test]
    public async Task Rewrite_KeepAliveHeader_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Keep-Alive", "timeout=5");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Keep-Alive")).IsFalse();
    }

    [Test]
    public async Task Rewrite_ProxyAuthenticate_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Proxy-Authenticate", "Basic realm=\"x\"");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Proxy-Authenticate")).IsFalse();
    }

    [Test]
    public async Task Rewrite_ProxyAuthorization_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Proxy-Authorization", "Basic foo");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Proxy-Authorization")).IsFalse();
    }

    [Test]
    public async Task Rewrite_ProxyConnection_IsStripped()
    {
        var headers = HeaderCollection.Empty.Add("Proxy-Connection", "close");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Proxy-Connection")).IsFalse();
    }

    [Test]
    public async Task Rewrite_ConnectionListedHeader_IsStripped()
    {
        var headers = HeaderCollection.Empty
            .Add("Connection", "X-Custom-Hop, close")
            .Add("X-Custom-Hop", "value");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("X-Custom-Hop")).IsFalse();
        await Assert.That(rewritten.Headers.HasHeader("Connection")).IsFalse();
    }

    [Test]
    public async Task Rewrite_RegularHeader_IsPreserved()
    {
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "application/json")
            .Add("Cache-Control", "no-cache");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.Get("Content-Type")).IsEqualTo("application/json");
        await Assert.That(rewritten.Headers.Get("Cache-Control")).IsEqualTo("no-cache");
    }

    [Test]
    public async Task Rewrite_MultiValueHeader_AllValuesPreserved()
    {
        var headers = HeaderCollection.Empty
            .Add("Set-Cookie", "a=1")
            .Add("Set-Cookie", "b=2");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        var cookies = rewritten.Headers.First(h => string.Equals(h.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase)).Value;
        await Assert.That(cookies.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Rewrite_StatusAndBody_ArePreserved()
    {
        var body = new byte[] { 1, 2, 3 };
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.StatusCode).IsEqualTo(200);
        await Assert.That(rewritten.ReasonPhrase).IsEqualTo("OK");
        await Assert.That(rewritten.Version).IsEqualTo("HTTP/1.1");
        await Assert.That(rewritten.Body.ToArray()).IsEquivalentTo(body);
    }

    [Test]
    public async Task Rewrite_AnyResponse_ReturnsNewInstance()
    {
        var response = CreateResponse(HeaderCollection.Empty);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten).IsNotSameReferenceAs(response);
    }

    [Test]
    public async Task Rewrite_CaseInsensitiveHopByHop_StripsRegardlessOfCase()
    {
        var headers = HeaderCollection.Empty.Add("connection", "close");
        var response = CreateResponse(headers);

        var rewritten = ForwardedResponseRewriter.Rewrite(response);

        await Assert.That(rewritten.Headers.HasHeader("Connection")).IsFalse();
    }

    private static HypertextTransferProtocolResponseData CreateResponse(HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
