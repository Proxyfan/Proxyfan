namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Identifies the strategy used to compare a target URL against a pattern.
/// </summary>
public enum MatchingRuleKind
{
    /// <summary>
    ///     The pattern must match the URL exactly (case-insensitive for host, case-sensitive for path).
    /// </summary>
    Exact,

    /// <summary>
    ///     The pattern uses wildcard syntax: <c>*</c> matches any sequence of characters and
    ///     <c>?</c> matches a single character.
    /// </summary>
    Wildcard,

    /// <summary>
    ///     The pattern is a regular expression evaluated with <see cref="System.Text.RegularExpressions.RegexOptions.Compiled" />
    ///     and a one-second timeout.
    /// </summary>
    Regex,
}
