using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="NoCachingRule" />.
/// </summary>
public sealed class NoCachingRuleTests
{
    /// <summary>
    ///     Verifies that the rule strips cache-control headers from a matching request and
    ///     replaces with no-cache.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingPattern_StripsCacheHeadersAndAddsNoCache()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var rule = new NoCachingRule(matching, isEnabled: true, priority: 0);
        var requestHeaders = HeaderCollection.Empty
            .Add("Host", "example.com")
            .Add("If-Modified-Since", "Wed, 21 Oct 2015 07:28:00 GMT")
            .Add("Cache-Control", "max-age=3600")
            .Add("If-None-Match", "etag-value");
        var request = CreateRequest("https://example.com/", requestHeaders);

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.ModifyRequest>();
        var modified = ((RequestPipelineAction.ModifyRequest)action!).ModifiedRequest;
        await Assert.That(modified.Headers.HasHeader("If-Modified-Since")).IsFalse();
        await Assert.That(modified.Headers.HasHeader("If-None-Match")).IsFalse();
        await Assert.That(modified.Headers.Get("Cache-Control")).IsEqualTo("no-cache");
    }

    /// <summary>
    ///     Verifies that the rule strips cache headers from a matching response.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_MatchingPattern_StripsCacheHeadersAndAddsNoCache()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var rule = new NoCachingRule(matching, isEnabled: true, priority: 0);
        var responseHeaders = HeaderCollection.Empty
            .Add("ETag", "v1")
            .Add("Last-Modified", "Wed, 21 Oct 2015 07:28:00 GMT")
            .Add("Cache-Control", "max-age=3600");
        var request = CreateRequest("https://example.com/", HeaderCollection.Empty);
        var response = CreateResponse(200, responseHeaders);

        var action = rule.EvaluateResponse(request, response);

        await Assert.That(action).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        var modified = ((ResponsePipelineAction.ModifyResponse)action!).ModifiedResponse;
        await Assert.That(modified.Headers.HasHeader("ETag")).IsFalse();
        await Assert.That(modified.Headers.HasHeader("Last-Modified")).IsFalse();
        await Assert.That(modified.Headers.Get("Cache-Control")).IsEqualTo("no-cache, no-store, must-revalidate");
    }

    /// <summary>
    ///     Verifies that a non-matching request is left untouched.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingPattern_ReturnsNull()
    {
        var matching = new MatchingRule("https://only-this.example/*", MatchingRuleKind.Wildcard);
        var rule = new NoCachingRule(matching, isEnabled: true, priority: 0);
        var request = CreateRequest("https://other.example/", HeaderCollection.Empty);

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Verifies that a non-matching response is left untouched.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_NonMatchingPattern_ReturnsNull()
    {
        var matching = new MatchingRule("https://only-this.example/*", MatchingRuleKind.Wildcard);
        var rule = new NoCachingRule(matching, isEnabled: true, priority: 0);
        var request = CreateRequest("https://other.example/", HeaderCollection.Empty);
        var response = CreateResponse(200, HeaderCollection.Empty);

        var action = rule.EvaluateResponse(request, response);

        await Assert.That(action).IsNull();
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url, HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData CreateResponse(int statusCode, HeaderCollection headers)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
