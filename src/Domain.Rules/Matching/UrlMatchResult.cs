namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Represents the outcome of evaluating a URL against a matcher pattern.
/// </summary>
public enum UrlMatchResult
{
    /// <summary>
    ///     The URL did not match the pattern.
    /// </summary>
    NoMatch,

    /// <summary>
    ///     The URL matched the pattern.
    /// </summary>
    Match,

    /// <summary>
    ///     The matcher could not determine whether the URL matched the pattern.
    /// </summary>
    Indeterminate,
}
