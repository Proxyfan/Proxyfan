using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Net.Http;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Projects an <see cref="HttpResponseMessage" />'s response and content headers into the
///     Proxyfan <see cref="HeaderCollection" /> wire format. Lives in a separate static helper
///     so it can be unit-tested independently of the network layer.
/// </summary>
public static class ComposerResponseHeaderProjector
{
    /// <summary>
    ///     Projects the response and content headers of <paramref name="response" /> into a
    ///     <see cref="HeaderCollection" />. The first value of each multi-valued header is kept
    ///     (matching the rest of the codebase which uses single-value semantics).
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>A populated <see cref="HeaderCollection" />.</returns>
    public static HeaderCollection Project(HttpResponseMessage response)
    {
        var headers = HeaderCollection.Empty;

        foreach (var header in response.Headers)
        {
            var first = FirstOrNull(header.Value);

            if (first is not null)
            {
                headers = headers.Add(header.Key, first);
            }
        }

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
            {
                var first = FirstOrNull(header.Value);

                if (first is not null)
                {
                    headers = headers.Add(header.Key, first);
                }
            }
        }

        return headers;
    }

    private static string? FirstOrNull(IEnumerable<string> values)
    {
        using var enumerator = values.GetEnumerator();

        if (enumerator.MoveNext())
        {
            return enumerator.Current;
        }

        return null;
    }
}
