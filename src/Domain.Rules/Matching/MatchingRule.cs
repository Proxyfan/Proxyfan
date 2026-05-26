using System;

namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Value object that captures the URL pattern and the strategy used to compare URLs against it.
///     Use <see cref="Compile" /> to obtain an executable <see cref="IUrlMatcher" />.
/// </summary>
public sealed class MatchingRule
{
    /// <summary>
    ///     Gets the strategy used to compare URLs against <see cref="Pattern" />.
    /// </summary>
    public MatchingRuleKind Kind { get; }

    /// <summary>
    ///     Gets the URL pattern.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    ///     Initializes a new <see cref="MatchingRule" /> with the supplied pattern and strategy.
    /// </summary>
    /// <param name="pattern">The URL pattern.</param>
    /// <param name="kind">The strategy used to compare URLs against the pattern.</param>
    public MatchingRule(string pattern, MatchingRuleKind kind)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern must be provided.", nameof(pattern));
        }

        Pattern = pattern;
        Kind = kind;
    }

    /// <summary>
    ///     Builds a fresh <see cref="IUrlMatcher" /> implementation that evaluates URLs against this rule.
    /// </summary>
    /// <returns>An <see cref="IUrlMatcher" /> compatible with this rule's pattern and kind.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <see cref="Kind" /> does not correspond to a known matcher implementation.
    /// </exception>
    public IUrlMatcher Compile()
    {
        return Kind switch
        {
            MatchingRuleKind.Exact => new ExactUrlMatcher(Pattern),
            MatchingRuleKind.Wildcard => new WildcardUrlMatcher(Pattern),
            MatchingRuleKind.Regex => new RegexUrlMatcher(Pattern),
            _ => throw new InvalidOperationException($"Unknown matching rule kind: {Kind}."),
        };
    }
}
