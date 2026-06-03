using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

    private static void FlushDirective(StringBuilder builder, List<string> directives)
    {
        var directive = builder.ToString().Trim();
        builder.Clear();

        if (!string.IsNullOrEmpty(directive))
        {
            directives.Add(directive);
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
        var value = StripQuotes(directive[(equalsIndex + 1)..]);

        if (string.Equals(name, "no-cache", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsNoCache = true;
        }
        else if (string.Equals(name, "private", StringComparison.OrdinalIgnoreCase))
        {
            parameters.IsPrivate = true;
        }
        else if (string.Equals(name, "max-age", StringComparison.OrdinalIgnoreCase)
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
        var directives = new List<string>();
        var builder = new StringBuilder(headerValue.Length);
        var isInsideQuotes = false;
        var isEscaped = false;

        foreach (var character in headerValue)
        {
            if (isEscaped)
            {
                builder.Append(character);
                isEscaped = false;
                continue;
            }

            if (character == '\\' && isInsideQuotes)
            {
                builder.Append(character);
                isEscaped = true;
                continue;
            }

            if (character == '"')
            {
                builder.Append(character);
                isInsideQuotes = !isInsideQuotes;
                continue;
            }

            if (character == ',' && !isInsideQuotes)
            {
                FlushDirective(builder, directives);
                continue;
            }

            builder.Append(character);
        }

        FlushDirective(builder, directives);

        return [.. directives];
    }

    private static string StripQuotes(string value)
    {
        value = value.Trim();

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1];
        }

        return value;
    }
}
