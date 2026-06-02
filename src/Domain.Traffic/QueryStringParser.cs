using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parses HTTP URL query strings into individual percent-decoded
///     <see cref="QueryParameter" /> pairs. Treats <c>+</c> as space per
///     <c>application/x-www-form-urlencoded</c> conventions.
/// </summary>
public static class QueryStringParser
{
    /// <summary>
    ///     Parses the query portion of the supplied URI. Returns an empty list when the URI
    ///     has no query.
    /// </summary>
    /// <param name="requestUri">The URI to extract the query from.</param>
    /// <returns>The parsed parameters in the order they appeared.</returns>
    public static IReadOnlyList<QueryParameter> Parse(Uri requestUri)
    {
        if (requestUri is null)
        {
            return [];
        }

        string query;

        if (requestUri.IsAbsoluteUri)
        {
            query = requestUri.Query;
        }
        else
        {
            var raw = requestUri.OriginalString;
            var queryIndex = raw.IndexOf('?', StringComparison.Ordinal);

            if (queryIndex < 0)
            {
                query = string.Empty;
            }
            else
            {
                query = raw[queryIndex..];
            }
        }

        return ParseQueryString(query);
    }

    /// <summary>
    ///     Parses raw <c>application/x-www-form-urlencoded</c> name/value pairs without
    ///     stripping any leading <c>?</c>. Form bodies, unlike URI query strings, do not
    ///     have a leading question-mark delimiter, so a first field name beginning with
    ///     <c>?</c> must be preserved verbatim.
    /// </summary>
    /// <param name="encodedPairs">The raw encoded pairs (e.g. <c>a=1&amp;b=2</c>).</param>
    /// <returns>The parsed parameters in the order they appeared.</returns>
    public static IReadOnlyList<QueryParameter> ParseEncodedPairs(string encodedPairs)
    {
        if (string.IsNullOrEmpty(encodedPairs))
        {
            return [];
        }

        var parameters = new List<QueryParameter>();
        var segments = encodedPairs.Split('&');

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                continue;
            }

            var equalsIndex = segment.IndexOf('=', StringComparison.Ordinal);

            string rawName;
            string rawValue;

            if (equalsIndex < 0)
            {
                rawName = segment;
                rawValue = string.Empty;
            }
            else
            {
                rawName = segment[..equalsIndex];
                rawValue = segment[(equalsIndex + 1)..];
            }

            var name = Decode(rawName);
            var value = Decode(rawValue);
            var parameter = new QueryParameter(name, value);
            parameters.Add(parameter);
        }

        return parameters;
    }

    /// <summary>
    ///     Parses the raw query string (optionally beginning with <c>?</c>) into individual
    ///     parameters.
    /// </summary>
    /// <param name="queryString">The raw query string.</param>
    /// <returns>The parsed parameters in the order they appeared.</returns>
    public static IReadOnlyList<QueryParameter> ParseQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return [];
        }

        var trimmed = queryString;

        if (trimmed[0] == '?')
        {
            trimmed = trimmed[1..];
        }

        return ParseEncodedPairs(trimmed);
    }

    private static string Decode(string raw)
    {
        if (raw.Length == 0)
        {
            return raw;
        }

        var spaceNormalized = raw.Replace('+', ' ');
        return Uri.UnescapeDataString(spaceNormalized);
    }
}
