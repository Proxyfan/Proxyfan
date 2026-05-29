using System;
using System.Text.RegularExpressions;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     Validates user-supplied DNS override patterns. Accepts exact host names
///     (e.g. <c>api.example.com</c>) and wildcard subdomain patterns
///     (e.g. <c>*.example.com</c>). Wildcards may only appear as the leading label.
/// </summary>
public static class DomainPatternValidator
{
    private static readonly Regex PatternRegex;

    static DomainPatternValidator()
    {
        const string pattern = @"^(\*\.)?[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?)*$";
        var matchTimeout = TimeSpan.FromSeconds(1);
        var compiled = new Regex(pattern, RegexOptions.Compiled, matchTimeout);
        PatternRegex = compiled;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="pattern" /> is a valid
    ///     exact-host-name or leading-wildcard subdomain pattern. Returns
    ///     <see langword="false" /> for null, whitespace-only, or syntactically invalid
    ///     patterns.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    /// <returns><see langword="true" /> when the pattern is valid.</returns>
    public static bool HasValidPattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var trimmed = pattern.Trim();
        try
        {
            return PatternRegex.IsMatch(trimmed);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
