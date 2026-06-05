using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests;

/// <summary>
///     Tests for <see cref="MutableBlockListRule" />.
/// </summary>
public sealed class MutableBlockListRuleTests
{
    /// <summary>
    ///     An empty rule does not match any URL.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NoPatterns_ReturnsNull()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var request = CreateRequest("https://example.com/");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     A URL matching a registered pattern produces a <see cref="RequestPipelineAction.Block" /> action.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_MatchingPattern_ReturnsBlock()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://blocked.example.com/*", MatchingRuleKind.Wildcard));
        var request = CreateRequest("https://blocked.example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsTypeOf<RequestPipelineAction.Block>();
    }

    /// <summary>
    ///     A URL not matching any pattern returns null.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_NonMatchingPattern_ReturnsNull()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://blocked.example.com/*", MatchingRuleKind.Wildcard));
        var request = CreateRequest("https://allowed.example.com/path");

        var action = rule.EvaluateRequest(request);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Adding the same pattern twice (same text and kind) keeps the registry at size one.
    /// </summary>
    [Test]
    public async Task AddPattern_Duplicate_IsIgnored()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var pattern = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);

        rule.AddPattern(pattern);
        rule.AddPattern(pattern);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Adding a pattern raises <see cref="MutableBlockListRule.Changed" />.
    /// </summary>
    [Test]
    public async Task AddPattern_NewPattern_RaisesChanged()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        rule.AddPattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     Adding a duplicate pattern does not raise <see cref="MutableBlockListRule.Changed" />.
    /// </summary>
    [Test]
    public async Task AddPattern_Duplicate_DoesNotRaiseChanged()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var pattern = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        rule.AddPattern(pattern);
        var count = 0;
        rule.Changed += () => count++;

        rule.AddPattern(pattern);

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing a pattern that was registered makes EvaluateRequest stop matching it.
    /// </summary>
    [Test]
    public async Task RemovePattern_RegisteredPattern_StopsMatching()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var pattern = new MatchingRule("https://blocked.example.com/*", MatchingRuleKind.Wildcard);
        rule.AddPattern(pattern);

        rule.RemovePattern(pattern);

        var action = rule.EvaluateRequest(CreateRequest("https://blocked.example.com/path"));
        await Assert.That(action).IsNull();
        await Assert.That(rule.GetPatterns().Count).IsEqualTo(0);
    }

    /// <summary>
    ///     AddPattern walks past non-matching entries when checking for duplicates.
    /// </summary>
    [Test]
    public async Task AddPattern_DuplicateAfterNonMatching_KeepsRegistrySize()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: false);
        var first = new MatchingRule("https://a.example.com/*", MatchingRuleKind.Wildcard);
        var second = new MatchingRule("https://b.example.com/*", MatchingRuleKind.Wildcard);

        rule.AddPattern(first);
        rule.AddPattern(second);
        rule.AddPattern(second);

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(2);
    }

    /// <summary>
    ///     RemovePattern walks past non-matching entries to remove the target.
    /// </summary>
    [Test]
    public async Task RemovePattern_TargetAfterNonMatching_RemovesTarget()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: false);
        var first = new MatchingRule("https://a.example.com/*", MatchingRuleKind.Wildcard);
        var second = new MatchingRule("https://b.example.com/*", MatchingRuleKind.Wildcard);
        rule.AddPattern(first);
        rule.AddPattern(second);

        rule.RemovePattern(second);

        var remaining = rule.GetPatterns();
        await Assert.That(remaining.Count).IsEqualTo(1);
        await Assert.That(remaining[0].Pattern).IsEqualTo(first.Pattern);
    }

    /// <summary>
    ///     Removing a previously-registered pattern raises <see cref="MutableBlockListRule.Changed" />.
    /// </summary>
    [Test]
    public async Task RemovePattern_RegisteredPattern_RaisesChanged()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var pattern = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);
        rule.AddPattern(pattern);
        var count = 0;
        rule.Changed += () => count++;

        rule.RemovePattern(pattern);

        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     Removing an unknown pattern does not raise <see cref="MutableBlockListRule.Changed" />.
    /// </summary>
    [Test]
    public async Task RemovePattern_UnknownPattern_DoesNotRaiseChanged()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        rule.RemovePattern(new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard));

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     Toggling the enabled flag updates <see cref="MutableBlockListRule.IsEnabled" /> and
    ///     raises the Changed event.
    /// </summary>
    [Test]
    public async Task SetEnabled_TogglesValue_RaisesChanged()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        rule.SetEnabled(isEnabled: false);

        await Assert.That(rule.IsEnabled).IsFalse();
        await Assert.That(count).IsEqualTo(1);
    }

    /// <summary>
    ///     Setting the same enabled value does not raise <see cref="MutableBlockListRule.Changed" />.
    /// </summary>
    [Test]
    public async Task SetEnabled_NoChange_DoesNotRaiseChanged()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var count = 0;
        rule.Changed += () => count++;

        rule.SetEnabled(isEnabled: true);

        await Assert.That(count).IsEqualTo(0);
    }

    /// <summary>
    ///     GetPatterns returns a defensive snapshot — mutating the rule does not affect prior snapshots.
    /// </summary>
    [Test]
    public async Task GetPatterns_AfterMutation_ReturnsDefensiveSnapshot()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        rule.AddPattern(new MatchingRule("https://a.example.com/*", MatchingRuleKind.Wildcard));

        var snapshot = rule.GetPatterns();
        rule.AddPattern(new MatchingRule("https://b.example.com/*", MatchingRuleKind.Wildcard));

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(rule.GetPatterns().Count).IsEqualTo(2);
    }

    /// <summary>
    ///     A malformed matcher pattern that fails to compile does not poison the rule:
    ///     the patterns collection, compiled matcher snapshot, and Changed event are unaffected.
    /// </summary>
    [Test]
    public async Task AddPattern_MalformedMatcher_DoesNotMutateState()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        var valid = new MatchingRule("https://blocked.example.com/*", MatchingRuleKind.Wildcard);
        rule.AddPattern(valid);
        var count = 0;
        rule.Changed += () => count++;
        var malformed = new MatchingRule("[invalid", MatchingRuleKind.Regex);

        await Assert.That(() => rule.AddPattern(malformed)).Throws<ArgumentException>();

        await Assert.That(rule.GetPatterns().Count).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(0);
        var action = rule.EvaluateRequest(CreateRequest("https://blocked.example.com/path"));
        await Assert.That(action).IsTypeOf<RequestPipelineAction.Block>();
    }

    /// <summary>
    ///     A regex timeout fails closed and still produces a block action.
    /// </summary>
    [Test]
    public async Task EvaluateRequest_RegexTimeout_ReturnsBlock()
    {
        var rule = new MutableBlockListRule(priority: 100, isEnabled: true);
        rule.AddPattern(new MatchingRule(@"^https://example\.com/(a+)+$", MatchingRuleKind.Regex));

        var action = rule.EvaluateRequest(CreateRequest($"https://example.com/{new string('a', 30)}X"));

        await Assert.That(action).IsTypeOf<RequestPipelineAction.Block>();
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
