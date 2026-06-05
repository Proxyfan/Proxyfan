using System;
using System.Text.RegularExpressions;

namespace Proxyfan.Domain.Rules.Matching;

/// <summary>
///     Compares URLs against a regular expression with ReDoS protection (one-second timeout).
/// </summary>
public sealed class RegexUrlMatcher : IUrlMatcher
{
    private static readonly TimeSpan MatchTimeout;
    private readonly Regex _compiledRegex;

    static RegexUrlMatcher()
    {
        MatchTimeout = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    ///     Initializes a new <see cref="RegexUrlMatcher" /> from a regular expression pattern.
    /// </summary>
    /// <param name="pattern">A .NET regular expression pattern (case-insensitive).</param>
    public RegexUrlMatcher(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern must be provided.", nameof(pattern));
        }

        var compiledRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, MatchTimeout);
        _compiledRegex = compiledRegex;
    }

    /// <inheritdoc />
    public UrlMatchResult GetMatchResult(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return UrlMatchResult.NoMatch;
        }

        try
        {
            return _compiledRegex.IsMatch(url)
                ? UrlMatchResult.Match
                : UrlMatchResult.NoMatch;
        }
        catch (RegexMatchTimeoutException)
        {
            return UrlMatchResult.Indeterminate;
        }
    }

    /// <inheritdoc />
    public bool HasMatch(string url)
    {
        return GetMatchResult(url) == UrlMatchResult.Match;
    }
}
