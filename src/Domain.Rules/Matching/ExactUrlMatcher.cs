using System;

namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Compares URLs against an exact pattern (case-insensitive for host, case-sensitive for path).
/// </summary>
public sealed class ExactUrlMatcher : IUrlMatcher
{
    private readonly string _pattern;

    /// <summary>
    ///     Initializes a new <see cref="ExactUrlMatcher" /> from a literal URL pattern.
    /// </summary>
    /// <param name="pattern">The literal URL pattern to compare against.</param>
    public ExactUrlMatcher(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern must be provided.", nameof(pattern));
        }

        _pattern = pattern;
    }

    /// <inheritdoc />
    public bool HasMatch(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        return string.Equals(_pattern, url, StringComparison.OrdinalIgnoreCase);
    }
}
