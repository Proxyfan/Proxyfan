using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Rules;

/// <summary>
///     Tests for <see cref="AllowListRule" />.
/// </summary>
public sealed class AllowListRuleTests
{
    /// <summary>
    ///     Verifies that the rule allows requests matching one of the patterns.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingPattern_ReturnsNull()
    {
        var rules = new[]
        {
            new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard),
        };
        var allowList = new AllowListRule(rules, isEnabled: true, priority: 0);
        var request = CreateRequest("https://example.com/path");

        var action = allowList.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Verifies that the rule blocks requests not matching any pattern.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingPattern_ReturnsBlock()
    {
        var rules = new[]
        {
            new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard),
        };
        var allowList = new AllowListRule(rules, isEnabled: true, priority: 0);
        var request = CreateRequest("https://other.com/");

        var action = allowList.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.Block>();
    }

    /// <summary>
    ///     Verifies that the rule is a no-op when no patterns are configured.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_EmptyPatternList_ReturnsNull()
    {
        var allowList = new AllowListRule(System.Linq.Enumerable.Empty<MatchingRule>(), isEnabled: true, priority: 0);
        var request = CreateRequest("https://example.com/path");

        var action = allowList.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Verifies that the constructor stores enabled/priority.
    /// </summary>
    [Test]
    public async Task Constructor_WithValues_StoresEnabledAndPriority()
    {
        var rule = new AllowListRule(System.Linq.Enumerable.Empty<MatchingRule>(), isEnabled: false, priority: 5);

        await Assert.That(rule.IsEnabled).IsFalse();
        await Assert.That(rule.Priority).IsEqualTo(5);
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
