using System;

namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Compiles a wildcard pattern (using <c>*</c> and <c>?</c>) into a regular expression and
///     delegates to <see cref="RegexUrlMatcher" /> for evaluation.
/// </summary>
public sealed class WildcardUrlMatcher : IUrlMatcher
{
    private readonly RegexUrlMatcher _regexMatcher;

    /// <summary>
    ///     Initializes a new <see cref="WildcardUrlMatcher" /> from a wildcard pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern. <c>*</c> matches any sequence; <c>?</c> matches one character.</param>
    public WildcardUrlMatcher(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern must be provided.", nameof(pattern));
        }

        var regexPattern = WildcardPatternConverter.ConvertToRegexPattern(pattern);
        var regexMatcher = new RegexUrlMatcher(regexPattern);
        _regexMatcher = regexMatcher;
    }

    /// <inheritdoc />
    public bool HasMatch(string url)
    {
        return _regexMatcher.HasMatch(url);
    }
}
