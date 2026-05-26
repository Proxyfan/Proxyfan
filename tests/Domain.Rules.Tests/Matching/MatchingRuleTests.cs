using Proxyfan.Domain.Rules.Matching;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Tests.Matching;

/// <summary>
///     Tests for <see cref="MatchingRule" />.
/// </summary>
public sealed class MatchingRuleTests
{
    /// <summary>
    ///     Verifies the constructor rejects a null pattern.
    /// </summary>
    [Test]
    public async Task Constructor_WithNullPattern_Throws()
    {
        await Assert.That(() => _ = new MatchingRule(null!, MatchingRuleKind.Exact))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies the constructor rejects an empty pattern.
    /// </summary>
    [Test]
    public async Task Constructor_WithEmptyPattern_Throws()
    {
        await Assert.That(() => _ = new MatchingRule(string.Empty, MatchingRuleKind.Wildcard))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that the constructor stores the pattern and kind.
    /// </summary>
    [Test]
    public async Task Constructor_WithValues_StoresPatternAndKind()
    {
        var rule = new MatchingRule("https://example.com/*", MatchingRuleKind.Wildcard);

        await Assert.That(rule.Pattern).IsEqualTo("https://example.com/*");
        await Assert.That(rule.Kind).IsEqualTo(MatchingRuleKind.Wildcard);
    }

    /// <summary>
    ///     Verifies that <see cref="MatchingRule.Compile" /> with kind <see cref="MatchingRuleKind.Exact" /> returns an exact matcher.
    /// </summary>
    [Test]
    public async Task Compile_ExactKind_ReturnsExactMatcher()
    {
        var rule = new MatchingRule("https://example.com/", MatchingRuleKind.Exact);

        var matcher = rule.Compile();

        await Assert.That(matcher).IsTypeOf<ExactUrlMatcher>();
    }

    /// <summary>
    ///     Verifies that <see cref="MatchingRule.Compile" /> with kind <see cref="MatchingRuleKind.Wildcard" /> returns a wildcard matcher.
    /// </summary>
    [Test]
    public async Task Compile_WildcardKind_ReturnsWildcardMatcher()
    {
        var rule = new MatchingRule("https://*.example.com/*", MatchingRuleKind.Wildcard);

        var matcher = rule.Compile();

        await Assert.That(matcher).IsTypeOf<WildcardUrlMatcher>();
    }

    /// <summary>
    ///     Verifies that <see cref="MatchingRule.Compile" /> with kind <see cref="MatchingRuleKind.Regex" /> returns a regex matcher.
    /// </summary>
    [Test]
    public async Task Compile_RegexKind_ReturnsRegexMatcher()
    {
        var rule = new MatchingRule(".*", MatchingRuleKind.Regex);

        var matcher = rule.Compile();

        await Assert.That(matcher).IsTypeOf<RegexUrlMatcher>();
    }

    /// <summary>
    ///     Verifies that <see cref="MatchingRule.Compile" /> with an unknown kind throws.
    /// </summary>
    [Test]
    public async Task Compile_UnknownKind_Throws()
    {
        var rule = new MatchingRule("x", (MatchingRuleKind)999);

        await Assert.That(rule.Compile).Throws<InvalidOperationException>();
    }
}
