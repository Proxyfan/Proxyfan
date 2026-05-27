using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="MutableAllowListRule" />.
/// </summary>
public sealed class MutableAllowListRuleTests
{
    /// <summary>
    ///     An empty allow list passes through all requests.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NoPatterns_ReturnsNull()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: true);
        var request = CreateRequest("https://example.com/");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     With at least one pattern, a non-matching request is blocked.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingPattern_ReturnsBlock()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://allowed.example.com/*", MatchingRuleKind.Wildcard));
        var request = CreateRequest("https://other.example.com/");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.Block>();
    }

    /// <summary>
    ///     A matching request passes through (returns null).
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingPattern_ReturnsNull()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://allowed.example.com/*", MatchingRuleKind.Wildcard));
        var request = CreateRequest("https://allowed.example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     AddPattern is idempotent for duplicate patterns.
    /// </summary>
    [Test]
    public async Task AddPattern_Duplicate_IsIgnored()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var pattern = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);

        rule.AddPattern(pattern);
        rule.AddPattern(pattern);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(1);
    }

    /// <summary>
    ///     AddPattern raises the Changed event on the first add only.
    /// </summary>
    [Test]
    public async Task AddPattern_Duplicate_OnlyRaisesChangedOnce()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var pattern = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        var count = 0;
        rule.Changed += () => count++;

        rule.AddPattern(pattern);
        rule.AddPattern(pattern);

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     RemovePattern removes a registered pattern.
    /// </summary>
    [Test]
    public async Task RemovePattern_RegisteredPattern_Removes()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: true);
        var pattern = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        rule.AddPattern(pattern);

        rule.RemovePattern(pattern);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     SetEnabled toggles IsEnabled and raises Changed.
    /// </summary>
    [Test]
    public async Task SetEnabled_TogglesValue_RaisesChanged()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        var count = 0;
        rule.Changed += () => count++;

        rule.SetEnabled(isEnabled: true);

        await Assert.That(rule.IsEnabled).IsTrue();
        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     SetEnabled to the same value is a no-op.
    /// </summary>
    [Test]
    public async Task SetEnabled_SameValue_IsNoOp()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        rule.SetEnabled(isEnabled: true);

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     GetPatterns returns a defensive snapshot.
    /// </summary>
    [Test]
    public async Task GetPatterns_AfterMutation_DoesNotAffectPriorSnapshot()
    {
        var rule = new MutableAllowListRule(priority: 50, isEnabled: false);
        rule.AddPattern(new MatchingRule("https://a.example.com/*", MatchingRuleKind.Wildcard));

        var snapshot = rule.GetPatterns();
        rule.AddPattern(new MatchingRule("https://b.example.com/*", MatchingRuleKind.Wildcard));

        await Assert.That(snapshot.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Priority is exposed via the IRule interface.
    /// </summary>
    [Test]
    public async Task Priority_AfterConstruction_ReturnsConstructorValue()
    {
        var rule = new MutableAllowListRule(priority: 42, isEnabled: false);

        await Assert.That(rule.Priority).IsEqualTo(42);
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
