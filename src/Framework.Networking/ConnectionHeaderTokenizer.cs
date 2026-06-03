using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Shared parser for the HTTP <c>Connection</c> header (RFC 7230 § 6.1). Splits the comma
///     separated token list so response rewriters can decide which referenced headers to strip
///     and which control tokens (e.g. <c>upgrade</c>) to preserve on the forwarded hop. Keeping
///     the tokeniser in one place avoids drift between <see cref="ForwardedResponseRewriter" />
///     and <see cref="UpgradeResponseRewriter" />.
/// </summary>
public static class ConnectionHeaderTokenizer
{
    /// <summary>
    ///     Returns the trimmed, comma-separated tokens of the <c>Connection</c> header on
    ///     <paramref name="headers" />, in their original case. Returns an empty array when the
    ///     header is absent or empty.
    /// </summary>
    /// <param name="headers">The response headers to inspect.</param>
    /// <returns>The parsed Connection tokens.</returns>
    public static string[] Parse(HeaderCollection headers)
    {
        var connection = headers.Get("Connection");

        if (string.IsNullOrEmpty(connection))
        {
            return [];
        }

        return connection.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
