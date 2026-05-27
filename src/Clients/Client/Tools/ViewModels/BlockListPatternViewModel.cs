using Proxyfan.Domain.Rules.Matching;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight read-only view model representing a single pattern entry in
///     the Block List or Allow List tool window. Exposes the underlying
///     <see cref="MatchingRule" /> for removal commands.
/// </summary>
public sealed class BlockListPatternViewModel
{
    /// <summary>
    ///     Gets the strategy used to compare URLs against <see cref="Pattern" />.
    /// </summary>
    public MatchingRuleKind Kind { get; }

    /// <summary>
    ///     Gets the URL pattern as a human-readable string.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    ///     Gets the underlying domain matching rule.
    /// </summary>
    public MatchingRule Rule { get; }

    /// <summary>
    ///     Initializes a new <see cref="BlockListPatternViewModel" /> wrapping a domain matching rule.
    /// </summary>
    /// <param name="rule">The matching rule to expose to the UI.</param>
    public BlockListPatternViewModel(MatchingRule rule)
    {
        Rule = rule;
        Pattern = rule.Pattern;
        Kind = rule.Kind;
    }
}
