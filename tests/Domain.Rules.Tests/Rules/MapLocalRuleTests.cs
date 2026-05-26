using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="MapLocalRule" />.
/// </summary>
public sealed class MapLocalRuleTests
{
    /// <summary>
    ///     Verifies that a matching request produces a <see cref="RequestPipelineAction.ServeLocalResponse" />.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingPattern_ServesLocalResponse()
    {
        var matching = new MatchingRule("https://example.com/api/*", MatchingRuleKind.Wildcard);
        var body = Encoding.UTF8.GetBytes("local response");
        var headers = new[] { new KeyValuePair<string, string>("Content-Type", "text/plain") };
        var parameters = new MapLocalRuleParameters
        {
            Body = body,
            Headers = headers,
            IsEnabled = true,
            Priority = 0,
            ReasonPhrase = "OK",
            StatusCode = 200,
        };
        var rule = new MapLocalRule(matching, parameters);
        var request = CreateRequest("https://example.com/api/users");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.ServeLocalResponse>();
        var localResponse = ((RequestPipelineAction.ServeLocalResponse)action!).LocalResponse;
        await Assert.That(localResponse.StatusCode).IsEqualTo(200);
        await Assert.That(localResponse.ReasonPhrase).IsEqualTo("OK");
        await Assert.That(localResponse.Headers.Get("Content-Type")).IsEqualTo("text/plain");
    }

    /// <summary>
    ///     Verifies that a non-matching request does nothing.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingPattern_ReturnsNull()
    {
        var matching = new MatchingRule("https://example.com/api/*", MatchingRuleKind.Wildcard);
        var rule = new MapLocalRule(matching, CreateDefaultParameters());
        var request = CreateRequest("https://other.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Verifies that the constructor rejects an invalid status code below 100.
    /// </summary>
    [Test]
    public async Task Constructor_WithStatusCodeBelowMin_Throws()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var parameters = new MapLocalRuleParameters
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            Priority = 0,
            ReasonPhrase = "?",
            StatusCode = 99,
        };

        await Assert.That(() => _ = new MapLocalRule(matching, parameters))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that the constructor rejects status codes above 599.
    /// </summary>
    [Test]
    public async Task Constructor_WithStatusCodeAboveMax_Throws()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var parameters = new MapLocalRuleParameters
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            Priority = 0,
            ReasonPhrase = "?",
            StatusCode = 600,
        };

        await Assert.That(() => _ = new MapLocalRule(matching, parameters))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that disabled rules retain the IsEnabled = false flag.
    /// </summary>
    [Test]
    public async Task Constructor_WithDisabledFlag_StoresIsEnabledAndPriority()
    {
        var matching = new MatchingRule("*", MatchingRuleKind.Wildcard);
        var parameters = new MapLocalRuleParameters
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = false,
            Priority = 7,
            ReasonPhrase = "OK",
            StatusCode = 200,
        };
        var rule = new MapLocalRule(matching, parameters);

        await Assert.That(rule.IsEnabled).IsFalse();
        await Assert.That(rule.Priority).IsEqualTo(7);
    }

    private static MapLocalRuleParameters CreateDefaultParameters()
    {
        return new MapLocalRuleParameters
        {
            Body = Array.Empty<byte>(),
            Headers = Array.Empty<KeyValuePair<string, string>>(),
            IsEnabled = true,
            Priority = 0,
            ReasonPhrase = "OK",
            StatusCode = 200,
        };
    }

    private static HypertextTransferProtocolRequestData CreateRequest(string url)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", new Uri(url).Host),
            Method = "GET",
            RequestUri = new Uri(url),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }
}
