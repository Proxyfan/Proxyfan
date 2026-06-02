using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Provides a static helper for producing a copy of a <see cref="HeaderCollection" /> with a
///     specified set of header names removed.
/// </summary>
public static class HeaderStripper
{
    /// <summary>
    ///     Returns a new <see cref="HeaderCollection" /> containing only those headers from
    ///     <paramref name="headers" /> whose name does not appear in <paramref name="headersToStrip" />
    ///     (case-insensitive comparison).
    /// </summary>
    /// <param name="headers">The source header collection.</param>
    /// <param name="headersToStrip">The list of header names to omit from the result.</param>
    /// <returns>A new header collection with the specified headers removed.</returns>
    public static HeaderCollection StripHeaders(HeaderCollection headers, IReadOnlyList<string> headersToStrip)
    {
        var builder = new HeaderCollection.Builder();

        foreach (var header in headers)
        {
            var shouldStrip = false;

            foreach (var name in headersToStrip)
            {
                if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    shouldStrip = true;
                    break;
                }
            }

            if (shouldStrip)
            {
                continue;
            }

            foreach (var value in header.Value)
            {
                builder.Add(header.Key, value);
            }
        }

        return builder.Build();
    }
}
