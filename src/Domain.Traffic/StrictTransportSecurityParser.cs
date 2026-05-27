using System;
using System.Globalization;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parser for the Strict-Transport-Security (HSTS) response header per RFC 6797.
/// </summary>
public static class StrictTransportSecurityParser
{
    /// <summary>
    ///     Parses the supplied HSTS header value. Returns null when the value is blank or
    ///     does not contain a usable max-age directive.
    /// </summary>
    /// <param name="headerValue">The raw Strict-Transport-Security header value.</param>
    /// <returns>The parsed directive, or null.</returns>
    public static StrictTransportSecurityDirective? Parse(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        var parts = headerValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        long? maxAge = null;
        var allowsSubDomains = false;
        var isPreloadable = false;

        foreach (var part in parts)
        {
            if (string.Equals(part, "includeSubDomains", StringComparison.OrdinalIgnoreCase))
            {
                allowsSubDomains = true;
                continue;
            }

            if (string.Equals(part, "preload", StringComparison.OrdinalIgnoreCase))
            {
                isPreloadable = true;
                continue;
            }

            var equalsIndex = part.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex <= 0)
            {
                continue;
            }

            var name = part[..equalsIndex];
            var value = StripQuotes(part[(equalsIndex + 1)..]);

            if (string.Equals(name, "max-age", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                && parsed >= 0)
            {
                maxAge = parsed;
            }
        }

        if (maxAge is null)
        {
            return null;
        }

        var directive = new StrictTransportSecurityDirective(maxAge.Value, allowsSubDomains, isPreloadable);
        return directive;
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1];
        }

        return value;
    }
}
