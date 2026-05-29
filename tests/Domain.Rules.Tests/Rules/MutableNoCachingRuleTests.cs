using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for the toggleable <see cref="MutableNoCachingRule" />.
/// </summary>
public sealed class MutableNoCachingRuleTests
{
    /// <summary>
    ///     When disabled the request-phase evaluation must skip and return null.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_Disabled_ReturnsNull()
    {
        var rule = new MutableNoCachingRule(priority: 100, isEnabled: false);
        var request = BuildRequest();

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     When enabled the request-phase evaluation must strip cache-related headers and inject no-cache.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_Enabled_StripsCacheHeadersAndInjectsNoCache()
    {
        var rule = new MutableNoCachingRule(priority: 100, isEnabled: true);
        var headers = HeaderCollection.Empty
            .Add("If-None-Match", "\"abc\"")
            .Add("If-Modified-Since", "Tue, 1 Jan 2025 00:00:00 GMT")
            .Add("Pragma", "no-cache")
            .Add("Authorization", "Bearer xyz");
        var request = BuildRequest(headers);

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.ModifyRequest>();
        var modified = ((RequestPipelineAction.ModifyRequest)action!).ModifiedRequest;
        await Assert.That(modified.Headers.HasHeader("If-None-Match")).IsFalse();
        await Assert.That(modified.Headers.HasHeader("If-Modified-Since")).IsFalse();
        await Assert.That(modified.Headers.HasHeader("Pragma")).IsFalse();
        await Assert.That(modified.Headers.HasHeader("Authorization")).IsTrue();
        await Assert.That(modified.Headers.Get("Cache-Control")).IsEqualTo("no-cache");
    }

    /// <summary>
    ///     When disabled the response-phase evaluation must skip and return null.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_Disabled_ReturnsNull()
    {
        var rule = new MutableNoCachingRule(priority: 100, isEnabled: false);
        var request = BuildRequest();
        var response = BuildResponse();

        var action = rule.EvaluateResponse(request, response);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     When enabled the response-phase evaluation must strip cache headers and inject conservative directives.
    /// </summary>
    [Test]
    public async Task EvaluateResponse_Enabled_StripsCacheHeadersAndInjectsDirectives()
    {
        var rule = new MutableNoCachingRule(priority: 100, isEnabled: true);
        var request = BuildRequest();
        var headers = HeaderCollection.Empty
            .Add("Cache-Control", "max-age=3600")
            .Add("ETag", "\"abc\"")
            .Add("Last-Modified", "Tue, 1 Jan 2025 00:00:00 GMT");
        var response = BuildResponse(headers);

        var action = rule.EvaluateResponse(request, response);

        await Assert.That(action).IsTypeOf<ResponsePipelineAction.ModifyResponse>();
        var modified = ((ResponsePipelineAction.ModifyResponse)action!).ModifiedResponse;
        await Assert.That(modified.Headers.HasHeader("ETag")).IsFalse();
        await Assert.That(modified.Headers.HasHeader("Last-Modified")).IsFalse();
        await Assert.That(modified.Headers.Get("Cache-Control")).IsEqualTo("no-cache, no-store, must-revalidate");
    }

    /// <summary>
    ///     SetEnabled toggles the IsEnabled flag and raises the Changed event when state changes.
    /// </summary>
    [Test]
    public async Task SetEnabled_StateChange_RaisesChangedEvent()
    {
        var rule = new MutableNoCachingRule(priority: 100, isEnabled: false);
        var changedCount = 0;
        rule.Changed += () => changedCount++;

        rule.SetEnabled(isEnabled: true);
        rule.SetEnabled(isEnabled: true);
        rule.SetEnabled(isEnabled: false);

        await Assert.That(rule.IsEnabled).IsFalse();
        await Assert.That(changedCount).IsEqualTo(2);
    }

    /// <summary>
    ///     The rule's priority reflects what was supplied at construction time.
    /// </summary>
    [Test]
    public async Task Priority_AfterConstruction_MatchesValueSupplied()
    {
        var rule = new MutableNoCachingRule(priority: 7, isEnabled: false);

        await Assert.That(rule.Priority).IsEqualTo(7);
    }

    private static HypertextTransferProtocolRequestData BuildRequest(HeaderCollection? headers = null)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers ?? HeaderCollection.Empty,
            Method = "GET",
            RequestUri = new Uri("https://example.com/index"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static HypertextTransferProtocolResponseData BuildResponse(HeaderCollection? headers = null)
    {
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers ?? HeaderCollection.Empty,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }
}
