using System;
using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parses <c>application/x-www-form-urlencoded</c> request bodies into individual
///     percent-decoded name/value pairs. Reuses the same percent/<c>+</c>-decoding semantics
///     as <see cref="QueryStringParser" />.
/// </summary>
public static class FormUrlEncodedParser
{
    /// <summary>
    ///     Parses the raw form body bytes (assumed UTF-8) into parameters.
    /// </summary>
    /// <param name="body">The raw form body bytes.</param>
    /// <returns>The parsed parameters in the order they appeared.</returns>
    public static IReadOnlyList<QueryParameter> Parse(ReadOnlyMemory<byte> body)
    {
        if (body.IsEmpty)
        {
            return [];
        }

        var text = Encoding.UTF8.GetString(body.Span);
        return QueryStringParser.ParseEncodedPairs(text);
    }

    /// <summary>
    ///     Parses the raw form body text into parameters.
    /// </summary>
    /// <param name="bodyText">The raw form body text.</param>
    /// <returns>The parsed parameters in the order they appeared.</returns>
    public static IReadOnlyList<QueryParameter> Parse(string bodyText)
    {
        if (string.IsNullOrEmpty(bodyText))
        {
            return [];
        }

        return QueryStringParser.ParseEncodedPairs(bodyText);
    }
}
