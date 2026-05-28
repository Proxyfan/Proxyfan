using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds the wire bytes for an HTTP request being forwarded directly to the origin server
///     (no upstream proxy chain). The rewriter performs the proxy-side responsibilities required
///     by RFC 7230:
///     <list type="bullet">
///         <item>Rewrites an absolute-URI request line to origin-form (just <c>/path?query</c>).</item>
///         <item>
///             Strips hop-by-hop headers (<c>Connection</c>, <c>Proxy-Connection</c>,
///             <c>Proxy-Authorization</c>, <c>Proxy-Authenticate</c>) and any header names listed in the
///             <c>Connection</c> header value, so credentials destined for an upstream proxy never leak to the
///             origin and connection-scoped controls do not propagate.
///         </item>
///         <item>
///             Appends a <c>Via: 1.1 proxyfan</c> token per RFC 7230 § 5.7.1, preserving any incoming
///             <c>Via</c> chain.
///         </item>
///     </list>
/// </summary>
public static class OriginRequestRewriter
{
    private const string ViaToken = "1.1 proxyfan";
    private static readonly HashSet<string> AlwaysStrippedHeaders;

    static OriginRequestRewriter()
    {
        var alwaysStripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Connection",
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
        };
        AlwaysStrippedHeaders = alwaysStripped;
    }

    /// <summary>
    ///     Returns the rewritten header bytes for forwarding directly to an origin server. The
    ///     body bytes are unchanged and should be written after the returned header bytes.
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data (used for method, URI, version).</param>
    /// <returns>The rewritten header bytes ending with the CRLF CRLF terminator.</returns>
    public static byte[] RewriteHeaders(ReadOnlyMemory<byte> originalHeaderBytes, HypertextTransferProtocolRequestData request)
    {
        var text = Encoding.ASCII.GetString(originalHeaderBytes.Span);
        var firstLineEnd = text.IndexOf("\r\n", StringComparison.Ordinal);

        if (firstLineEnd < 0)
        {
            return originalHeaderBytes.ToArray();
        }

        var originForm = BuildOriginForm(request);
        var rewrittenRequestLine = $"{request.Method} {originForm} {request.Version}";

        var headerSection = text[(firstLineEnd + 2)..];
        var headerLines = SplitHeaderLines(headerSection);
        var connectionListedNames = ExtractConnectionListedHeaderNames(headerLines);

        var rebuilt = new StringBuilder(text.Length + ViaToken.Length + 16);
        rebuilt.Append(rewrittenRequestLine);
        rebuilt.Append("\r\n");
        var viaAppendedInline = HasAppendedHeadersWithInlineVia(rebuilt, headerLines, connectionListedNames);

        if (!viaAppendedInline)
        {
            rebuilt.Append("Via: ");
            rebuilt.Append(ViaToken);
            rebuilt.Append("\r\n");
        }

        rebuilt.Append("\r\n");
        return Encoding.ASCII.GetBytes(rebuilt.ToString());
    }

    private static string BuildOriginForm(HypertextTransferProtocolRequestData request)
    {
        if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.RequestUri.OriginalString, "*", StringComparison.Ordinal))
        {
            return "*";
        }

        if (request.RequestUri.IsAbsoluteUri)
        {
            var pathAndQuery = request.RequestUri.PathAndQuery;
            return string.IsNullOrEmpty(pathAndQuery) ? "/" : pathAndQuery;
        }

        var originalPath = request.RequestUri.OriginalString;

        if (string.IsNullOrEmpty(originalPath))
        {
            return "/";
        }

        var fragmentIndex = originalPath.IndexOf('#');

        if (fragmentIndex >= 0)
        {
            originalPath = originalPath[..fragmentIndex];
        }

        return string.IsNullOrEmpty(originalPath) ? "/" : originalPath;
    }

    private static HashSet<string> ExtractConnectionListedHeaderNames(IReadOnlyList<string> headerLines)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in headerLines)
        {
            var colonIndex = line.IndexOf(':');

            if (colonIndex <= 0)
            {
                continue;
            }

            var name = line[..colonIndex].Trim();

            if (!string.Equals(name, "Connection", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = line[(colonIndex + 1)..].Trim();
            var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in tokens)
            {
                if (string.Equals(token, "close", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(token, "keep-alive", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                names.Add(token);
            }
        }

        return names;
    }

    private static bool HasAppendedHeadersWithInlineVia(
        StringBuilder rebuilt,
        IReadOnlyList<string> headerLines,
        HashSet<string> connectionListedNames)
    {
        var viaAppendedInline = false;

        foreach (var line in headerLines)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var colonIndex = line.IndexOf(':');

            if (colonIndex <= 0)
            {
                rebuilt.Append(line);
                rebuilt.Append("\r\n");
                continue;
            }

            var name = line[..colonIndex].Trim();

            if (AlwaysStrippedHeaders.Contains(name) || connectionListedNames.Contains(name))
            {
                continue;
            }

            if (string.Equals(name, "Via", StringComparison.OrdinalIgnoreCase))
            {
                rebuilt.Append(line.TrimEnd());
                rebuilt.Append(", ");
                rebuilt.Append(ViaToken);
                rebuilt.Append("\r\n");
                viaAppendedInline = true;
                continue;
            }

            rebuilt.Append(line);
            rebuilt.Append("\r\n");
        }

        return viaAppendedInline;
    }

    private static List<string> SplitHeaderLines(string headerSection)
    {
        var lines = headerSection.Split("\r\n", StringSplitOptions.None);
        var result = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            result.Add(line);
        }

        return result;
    }
}
