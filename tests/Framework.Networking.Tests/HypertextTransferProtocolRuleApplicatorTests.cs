using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolRuleApplicator" />.
/// </summary>
public sealed class HypertextTransferProtocolRuleApplicatorTests
{
    /// <summary>
    ///     Verifies that an empty action list returns the original request unchanged.
    /// </summary>
    [Test]
    public async Task ApplyRequestModifications_NoActions_ReturnsOriginalRequest()
    {
        var request = CreateRequest("https://example.com/");
        IReadOnlyList<RequestPipelineAction> actions = [];

        var result = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(request, actions);

        await Assert.That(result).IsSameReferenceAs(request);
    }

    /// <summary>
    ///     Verifies that a Block action does not modify the request (Block short-circuits at engine level).
    /// </summary>
    [Test]
    public async Task ApplyRequestModifications_BlockAction_ReturnsOriginalRequest()
    {
        var request = CreateRequest("https://example.com/");
        IReadOnlyList<RequestPipelineAction> actions = [new RequestPipelineAction.Block()];

        var result = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(request, actions);

        await Assert.That(result).IsSameReferenceAs(request);
    }

    /// <summary>
    ///     Verifies that a Redirect action returns the rewritten request.
    /// </summary>
    [Test]
    public async Task ApplyRequestModifications_RedirectAction_ReturnsRewrittenRequest()
    {
        var request = CreateRequest("https://example.com/");
        var rewritten = CreateRequest("https://other.com/");
        IReadOnlyList<RequestPipelineAction> actions = [new RequestPipelineAction.Redirect(rewritten)];

        var result = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(request, actions);

        await Assert.That(result).IsSameReferenceAs(rewritten);
    }

    /// <summary>
    ///     Verifies that a ModifyRequest action returns the modified request.
    /// </summary>
    [Test]
    public async Task ApplyRequestModifications_ModifyRequest_ReturnsModifiedRequest()
    {
        var request = CreateRequest("https://example.com/");
        var modified = CreateRequest("https://example.com/modified");
        IReadOnlyList<RequestPipelineAction> actions = [new RequestPipelineAction.ModifyRequest(modified)];

        var result = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(request, actions);

        await Assert.That(result).IsSameReferenceAs(modified);
    }

    /// <summary>
    ///     Verifies that ApplyResponseModifications returns the original response when no actions apply.
    /// </summary>
    [Test]
    public async Task ApplyResponseModifications_NoActions_ReturnsOriginalResponse()
    {
        var response = CreateResponse(200);
        IReadOnlyList<ResponsePipelineAction> actions = [];

        var result = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(response, actions);

        await Assert.That(result).IsSameReferenceAs(response);
    }

    /// <summary>
    ///     Verifies that a ModifyResponse action returns the modified response.
    /// </summary>
    [Test]
    public async Task ApplyResponseModifications_ModifyResponse_ReturnsModifiedResponse()
    {
        var response = CreateResponse(200);
        var modified = CreateResponse(304);
        IReadOnlyList<ResponsePipelineAction> actions = [new ResponsePipelineAction.ModifyResponse(modified)];

        var result = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(response, actions);

        await Assert.That(result).IsSameReferenceAs(modified);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.FindBlockingAction" /> returns
    ///     null when the action list contains no Block or ServeLocalResponse action.
    /// </summary>
    [Test]
    public async Task FindBlockingAction_NoBlockingActions_ReturnsNull()
    {
        var request = CreateRequest("https://example.com/");
        IReadOnlyList<RequestPipelineAction> actions = [new RequestPipelineAction.ModifyRequest(request)];

        var result = HypertextTransferProtocolRuleApplicator.FindBlockingAction(actions);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.FindBlockingAction" /> finds
    ///     the Block action.
    /// </summary>
    [Test]
    public async Task FindBlockingAction_WithBlock_ReturnsBlock()
    {
        IReadOnlyList<RequestPipelineAction> actions =
        [
            new RequestPipelineAction.ModifyRequest(CreateRequest("https://example.com/")),
            new RequestPipelineAction.Block(),
        ];

        var result = HypertextTransferProtocolRuleApplicator.FindBlockingAction(actions);

        await Assert.That(result).IsTypeOf<RequestPipelineAction.Block>();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.FindBlockingAction" /> finds
    ///     a ServeLocalResponse action.
    /// </summary>
    [Test]
    public async Task FindBlockingAction_WithServeLocalResponse_ReturnsServeLocalResponse()
    {
        var serveResponse = CreateResponse(200);
        IReadOnlyList<RequestPipelineAction> actions = [new RequestPipelineAction.ServeLocalResponse(serveResponse)];

        var result = HypertextTransferProtocolRuleApplicator.FindBlockingAction(actions);

        await Assert.That(result).IsTypeOf<RequestPipelineAction.ServeLocalResponse>();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.CreateBlockedResponseData" />
    ///     returns a 403 Forbidden response.
    /// </summary>
    [Test]
    public async Task CreateBlockedResponseData_WhenCalled_ReturnsForbiddenResponse()
    {
        var response = HypertextTransferProtocolRuleApplicator.CreateBlockedResponseData();

        await Assert.That(response.StatusCode).IsEqualTo(403);
        await Assert.That(response.ReasonPhrase).IsEqualTo("Forbidden");
        await Assert.That(response.Headers.Get("Content-Length")).IsEqualTo("0");
        await Assert.That(response.Headers.Get("Connection")).IsEqualTo("close");
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.BuildLocalResponseExchange" />
    ///     serializes the response into a writeable exchange.
    /// </summary>
    [Test]
    public async Task BuildLocalResponseExchange_ResponseWithHeadersAndBody_ProducesValidExchange()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "text/plain").Add("Content-Length", "5");
        var response = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.ASCII.GetBytes("hello"),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });

        var exchange = HypertextTransferProtocolRuleApplicator.BuildLocalResponseExchange(response);
        var headerText = Encoding.ASCII.GetString(exchange.HeaderBytes);

        await Assert.That(headerText.StartsWith("HTTP/1.1 200 OK\r\n", StringComparison.Ordinal)).IsTrue();
        await Assert.That(headerText.Contains("Content-Type: text/plain", StringComparison.Ordinal)).IsTrue();
        await Assert.That(headerText.EndsWith("\r\n\r\n", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith" />
    ///     returns the original exchange when the effective request is the same instance.
    /// </summary>
    [Test]
    public async Task BuildRequestExchangeWith_SameRequest_ReturnsOriginalExchange()
    {
        var request = CreateRequest("https://example.com/");
        var headerBytes = Encoding.ASCII.GetBytes("GET https://example.com/ HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var exchange = new HypertextTransferProtocolProxyRequestExchange(System.Array.Empty<byte>(), headerBytes, request);

        var result = HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith(exchange, request);

        await Assert.That(result).IsSameReferenceAs(exchange);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith" />
    ///     rebuilds the exchange when the effective request is different.
    /// </summary>
    [Test]
    public async Task BuildRequestExchangeWith_DifferentRequest_RebuildsHeaderBytes()
    {
        var originalRequest = CreateRequest("https://example.com/");
        var originalHeaderBytes = Encoding.ASCII.GetBytes("GET https://example.com/ HTTP/1.1\r\nHost: example.com\r\n\r\n");
        var originalExchange = new HypertextTransferProtocolProxyRequestExchange(System.Array.Empty<byte>(), originalHeaderBytes, originalRequest);
        var newRequest = CreateRequest("https://different.com/path");

        var result = HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith(originalExchange, newRequest);
        var headerText = Encoding.ASCII.GetString(result.HeaderBytes);

        await Assert.That(result).IsNotSameReferenceAs(originalExchange);
        await Assert.That(headerText.StartsWith("GET https://different.com/path HTTP/1.1\r\n", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith" />
    ///     returns the original exchange when the final response is the same instance.
    /// </summary>
    [Test]
    public async Task BuildResponseExchangeWith_SameResponse_ReturnsOriginalExchange()
    {
        var response = CreateResponse(200);
        var headerBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
        var exchange = new HypertextTransferProtocolProxyResponseExchange(System.Array.Empty<byte>(), headerBytes, response);

        var result = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(exchange, response);

        await Assert.That(result).IsSameReferenceAs(exchange);
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith" />
    ///     rebuilds the exchange when the final response is different.
    /// </summary>
    [Test]
    public async Task BuildResponseExchangeWith_DifferentResponse_RebuildsHeaderBytes()
    {
        var originalResponse = CreateResponse(200);
        var originalHeaderBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
        var originalExchange = new HypertextTransferProtocolProxyResponseExchange(System.Array.Empty<byte>(), originalHeaderBytes, originalResponse);
        var newResponse = CreateResponse(304);

        var result = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(originalExchange, newResponse);
        var headerText = Encoding.ASCII.GetString(result.HeaderBytes);

        await Assert.That(result).IsNotSameReferenceAs(originalExchange);
        await Assert.That(headerText.StartsWith("HTTP/1.1 304 OK\r\n", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="HypertextTransferProtocolRuleApplicator.SendBlockedResponseAsync" />
    ///     writes a 403 response and flushes.
    /// </summary>
    [Test]
    public async Task SendBlockedResponseAsync_WhenCalled_WritesForbiddenBytes()
    {
        var pipe = new Pipe();

        await HypertextTransferProtocolRuleApplicator.SendBlockedResponseAsync(pipe.Writer, CancellationToken.None);
        await pipe.Writer.CompleteAsync();

        using var memoryStream = new System.IO.MemoryStream();
        await pipe.Reader.AsStream().CopyToAsync(memoryStream);
        var text = Encoding.ASCII.GetString(memoryStream.ToArray());

        await Assert.That(text.StartsWith("HTTP/1.1 403 Forbidden", StringComparison.Ordinal)).IsTrue();
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = System.Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData CreateResponse(int statusCode)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = System.Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
