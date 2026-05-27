using System;

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

        var parts = rawValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var mediaType = parts[0];
        string? charset = null;
        string? boundary = null;

        for (var index = 1; index < parts.Length; index++)
        {
            var parameter = parts[index];
            var equalsIndex = parameter.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex <= 0)
            {
                continue;
            }

            var parameterName = parameter[..equalsIndex];
            var parameterValue = StripQuotes(parameter[(equalsIndex + 1)..]);

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

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1];
        }

        return value;
    }
}
