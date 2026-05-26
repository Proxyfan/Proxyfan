using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Helpers for rewriting the Host header to match a new destination URI.
/// </summary>
public static class MapRemoteHeaderRewriter
{
    /// <summary>
    ///     Returns a new <see cref="HeaderCollection" /> with the Host header replaced to match
    ///     the supplied URI's host and (non-default) port.
    /// </summary>
    /// <param name="headers">The source header collection.</param>
    /// <param name="rewrittenUri">The destination URI used to compute the new Host value.</param>
    /// <returns>The header collection with the Host value updated.</returns>
    public static HeaderCollection ReplaceHostHeader(HeaderCollection headers, Uri rewrittenUri)
    {
        var headersWithoutHost = HeaderCollection.Empty;

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var value in header.Value)
            {
                headersWithoutHost = headersWithoutHost.Add(header.Key, value);
            }
        }

        var newHostValue = rewrittenUri.IsDefaultPort
            ? rewrittenUri.Host
            : $"{rewrittenUri.Host}:{rewrittenUri.Port}";
        var headersWithNewHost = headersWithoutHost.Add("Host", newHostValue);
        return headersWithNewHost;
    }
}
