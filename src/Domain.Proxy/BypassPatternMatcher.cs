using System.Collections.Generic;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Wildcard host matcher used by <see cref="UpstreamProxyOptions" /> to decide whether a
///     destination should bypass the upstream proxy. Patterns support <c>*</c> (zero or more
///     characters) and <c>?</c> (exactly one character); matches are case-insensitive.
///     Empty/whitespace patterns are ignored.
/// </summary>
public static class BypassPatternMatcher
{
    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="host" /> matches any pattern in
    ///     <paramref name="patterns" /> using the wildcard rules described on the class.
    /// </summary>
    /// <param name="patterns">The configured bypass patterns. May be empty or contain blanks.</param>
    /// <param name="host">The destination host to test.</param>
    /// <returns><see langword="true" /> when bypass should be applied.</returns>
    public static bool HasMatch(IEnumerable<string> patterns, string host)
    {
        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            if (HasMatch(pattern, host))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanCharsMatch(char patternChar, char hostChar)
    {
        return char.ToLowerInvariant(patternChar) == char.ToLowerInvariant(hostChar);
    }

    private static bool HasMatch(string pattern, string host)
    {
        var patternIndex = 0;
        var hostIndex = 0;
        var starPatternIndex = -1;
        var starHostIndex = 0;

        while (hostIndex < host.Length)
        {
            if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starPatternIndex = patternIndex;
                starHostIndex = hostIndex;
                patternIndex++;
                continue;
            }

            if (patternIndex < pattern.Length
                && (pattern[patternIndex] == '?' || CanCharsMatch(pattern[patternIndex], host[hostIndex])))
            {
                patternIndex++;
                hostIndex++;
                continue;
            }

            if (starPatternIndex == -1)
            {
                return false;
            }

            patternIndex = starPatternIndex + 1;
            starHostIndex++;
            hostIndex = starHostIndex;
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}
