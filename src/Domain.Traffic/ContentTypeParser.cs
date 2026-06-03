using System;
using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parser for HTTP Content-Type header values per RFC 9110 § 8.3.
/// </summary>
public static class ContentTypeParser
{
    /// <summary>
    ///     Parses the supplied header value into a <see cref="ContentType" />. Returns null
    ///     when the value is blank.
    /// </summary>
    /// <param name="rawValue">The raw Content-Type header value.</param>
    /// <returns>The parsed type, or null.</returns>
    public static ContentType? Parse(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var parts = SplitRespectingQuotes(rawValue);
        if (parts.Count == 0)
        {
            return null;
        }

        var mediaType = parts[0];
        string? charset = null;
        string? boundary = null;

        for (var index = 1; index < parts.Count; index++)
        {
            var parameter = parts[index];
            var equalsIndex = parameter.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex <= 0)
            {
                continue;
            }

            var parameterName = parameter[..equalsIndex].Trim();
            var parameterValue = UnquoteValue(parameter[(equalsIndex + 1)..].Trim());

            if (string.Equals(parameterName, "charset", StringComparison.OrdinalIgnoreCase))
            {
                charset = parameterValue;
            }
            else if (string.Equals(parameterName, "boundary", StringComparison.OrdinalIgnoreCase))
            {
                boundary = parameterValue;
            }
        }

        var result = new ContentType(mediaType, charset, boundary, rawValue);
        return result;
    }

    private static void AppendPart(List<string> parts, StringBuilder current)
    {
        var trimmed = current.ToString().Trim();
        if (trimmed.Length > 0)
        {
            parts.Add(trimmed);
        }
    }

    private static List<string> SplitRespectingQuotes(string rawValue)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var index = 0;

        while (index < rawValue.Length)
        {
            var character = rawValue[index];

            if (inQuotes)
            {
                current.Append(character);
                if (character == '\\' && index + 1 < rawValue.Length)
                {
                    current.Append(rawValue[index + 1]);
                    index += 2;
                    continue;
                }

                if (character == '"')
                {
                    inQuotes = false;
                }

                index++;
                continue;
            }

            if (character == '"')
            {
                inQuotes = true;
                current.Append(character);
                index++;
                continue;
            }

            if (character == ';')
            {
                AppendPart(parts, current);
                current.Clear();
                index++;
                continue;
            }

            current.Append(character);
            index++;
        }

        AppendPart(parts, current);
        return parts;
    }

    private static string UnquoteValue(string value)
    {
        if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
        {
            return value;
        }

        var inner = value[1..^1];
        if (inner.IndexOf('\\', StringComparison.Ordinal) < 0)
        {
            return inner;
        }

        var builder = new StringBuilder(inner.Length);
        var index = 0;
        while (index < inner.Length)
        {
            var character = inner[index];
            if (character == '\\' && index + 1 < inner.Length)
            {
                builder.Append(inner[index + 1]);
                index += 2;
                continue;
            }

            builder.Append(character);
            index++;
        }

        return builder.ToString();
    }
}
