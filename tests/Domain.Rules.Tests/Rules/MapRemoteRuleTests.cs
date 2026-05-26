using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="MapRemoteRule" />.
/// </summary>
public sealed class MapRemoteRuleTests
{
    /// <summary>
    ///     Verifies that a matching request has its host rewritten.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingPattern_RewritesHost()
    {
        var matching = new MatchingRule("https://prod.example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(scheme: null, host: "localhost", port: 8080, path: null, isPreservingHostHeader: false);
        var rule = new MapRemoteRule(matching, destination, isEnabled: true, priority: 0);
        var request = CreateRequest("https://prod.example.com/api/users");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.Redirect>();
        var redirect = (RequestPipelineAction.Redirect)action!;
        await Assert.That(redirect.RewrittenRequest.RequestUri.Host).IsEqualTo("localhost");
        await Assert.That(redirect.RewrittenRequest.RequestUri.Port).IsEqualTo(8080);
        await Assert.That(redirect.RewrittenRequest.Headers.Get("Host")).IsEqualTo("localhost:8080");
    }

    /// <summary>
    ///     Verifies that a non-matching request is left untouched.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingPattern_ReturnsNull()
    {
        var matching = new MatchingRule("https://prod.example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(scheme: null, host: "localhost", port: 8080, path: null, isPreservingHostHeader: false);
        var rule = new MapRemoteRule(matching, destination, isEnabled: true, priority: 0);
        var request = CreateRequest("https://other.example.com/api");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Verifies that PreserveHostHeader keeps the original Host header.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_PreserveHostHeader_KeepsOriginalHost()
    {
        var matching = new MatchingRule("https://prod.example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(scheme: null, host: "localhost", port: 8080, path: null, isPreservingHostHeader: true);
        var rule = new MapRemoteRule(matching, destination, isEnabled: true, priority: 0);
        var request = CreateRequest("https://prod.example.com/api");

        var action = rule.EvaluateRequest(request);

        var redirect = (RequestPipelineAction.Redirect)action!;
        await Assert.That(redirect.RewrittenRequest.Headers.Get("Host")).IsEqualTo("prod.example.com");
    }

    /// <summary>
    ///     Verifies that the scheme can be rewritten.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_RewriteScheme_ChangesScheme()
    {
        var matching = new MatchingRule("https://prod.example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(scheme: "http", host: null, port: null, path: null, isPreservingHostHeader: false);
        var rule = new MapRemoteRule(matching, destination, isEnabled: true, priority: 0);
        var request = CreateRequest("https://prod.example.com/api");

        var action = rule.EvaluateRequest(request);

        var redirect = (RequestPipelineAction.Redirect)action!;
        await Assert.That(redirect.RewrittenRequest.RequestUri.Scheme).IsEqualTo("http");
    }

    /// <summary>
    ///     Verifies that the path can be rewritten.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_RewritePath_ChangesPath()
    {
        var matching = new MatchingRule("https://prod.example.com/*", MatchingRuleKind.Wildcard);
        var destination = new MapRemoteDestination(scheme: null, host: null, port: null, path: "/v2/users", isPreservingHostHeader: false);
        var rule = new MapRemoteRule(matching, destination, isEnabled: true, priority: 0);
        var request = CreateRequest("https://prod.example.com/api/users");

        var action = rule.EvaluateRequest(request);

        var redirect = (RequestPipelineAction.Redirect)action!;
        await Assert.That(redirect.RewrittenRequest.RequestUri.AbsolutePath).IsEqualTo("/v2/users");
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
