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
    public UrlMatchResult GetMatchResult(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return UrlMatchResult.NoMatch;
        }

        if (Uri.TryCreate(_pattern, UriKind.Absolute, out var patternUri)
            && Uri.TryCreate(url, UriKind.Absolute, out var candidateUri))
        {
            return string.Equals(patternUri.Scheme, candidateUri.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(patternUri.Host, candidateUri.Host, StringComparison.OrdinalIgnoreCase)
                && patternUri.Port == candidateUri.Port
                && string.Equals(patternUri.AbsolutePath, candidateUri.AbsolutePath, StringComparison.Ordinal)
                && string.Equals(patternUri.Query, candidateUri.Query, StringComparison.Ordinal)
                && string.Equals(patternUri.Fragment, candidateUri.Fragment, StringComparison.Ordinal)
                ? UrlMatchResult.Match
                : UrlMatchResult.NoMatch;
        }

        return string.Equals(_pattern, url, StringComparison.Ordinal)
            ? UrlMatchResult.Match
            : UrlMatchResult.NoMatch;
    }

    /// <inheritdoc />
    public bool HasMatch(string url)
    {
        return GetMatchResult(url) == UrlMatchResult.Match;
    }
}
