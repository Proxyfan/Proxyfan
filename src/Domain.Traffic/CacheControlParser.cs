using System;
using System.Globalization;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parser for HTTP Cache-Control header values per RFC 9111 Â§ 5.2.
/// </summary>
public static class CacheControlParser
{
    /// <summary>
    ///     Parses the supplied Cache-Control header value into structured directives. Returns
    ///     a directives object even for blank input (all flags will be false, ages null).
    /// </summary>
    /// <param name="headerValue">The raw Cache-Control header value.</param>
    /// <returns>The parsed directives.</returns>
    public static CacheControlDirectives Parse(string? headerValue)
    {
        var parameters = new CacheControlDirectivesParameters();

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return new CacheControlDirectives(parameters);
        }

        foreach (var part in SplitDirectives(headerValue))
        {
            ApplyDirective(parameters, part);
        }

        var directives = new CacheControlDirectives(parameters);
        return directives;
    }

    private static void AddDirectivePart(System.Collections.Generic.List<string> parts, string part)
    {
        var trimmed = part.Trim();

        if (trimmed.Length > 0)
        {
            parts.Add(trimmed);
        }
    }

    private static void ApplyDirective(CacheControlDirectivesParameters parameters, string directive)
    {
        var equalsIndex = directive.IndexOf('=', StringComparison.Ordinal);

        if (equalsIndex < 0)
        {
            ApplyFlag(parameters, directive);
            return;
        }

        var name = directive[..equalsIndex].Trim();
        var value = StripQuotes(directive[(equalsIndex + 1)..].Trim());

        if (string.Equals(name, "no-cache", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsNoCache = true;
            return;
        }

        if (string.Equals(name, "private", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsPrivate = true;
            return;
        }

        if (string.Equals(name, "max-age", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxAge)
            && maxAge >= 0)
        {
            parameters.MaxAgeSeconds = maxAge;
        }
        else if (string.Equals(name, "s-maxage", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedMaxAge)
            && sharedMaxAge >= 0)
        {
            parameters.SharedMaxAgeSeconds = sharedMaxAge;
        }
    }

    private static void ApplyFlag(CacheControlDirectivesParameters parameters, string flag)
    {
        if (string.Equals(flag, "no-cache", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsNoCache = true;
        }
        else if (string.Equals(flag, "no-store", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsNoStore = true;
        }
        else if (string.Equals(flag, "public", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsPublic = true;
        }
        else if (string.Equals(flag, "private", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsPrivate = true;
        }
        else if (string.Equals(flag, "must-revalidate", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsMustRevalidate = true;
        }
    }

    private static string[] SplitDirectives(string headerValue)
    {
        System.Collections.Generic.List<string> parts = [];
        var startIndex = 0;
        var inQuotes = false;
        var escaped = false;

        for (var index = 0; index < headerValue.Length; index++)
        {
            var character = headerValue[index];

            if (character == '"' && !escaped)
            {
                inQuotes = !inQuotes;
            }

            if (character == ',' && !inQuotes)
            {
                AddDirectivePart(parts, headerValue[startIndex..index]);
                startIndex = index + 1;
            }

            escaped = character == '\\' && inQuotes && !escaped;
        }

        AddDirectivePart(parts, headerValue[startIndex..]);
        return [.. parts];
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
