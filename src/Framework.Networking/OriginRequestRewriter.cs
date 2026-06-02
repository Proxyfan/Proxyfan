using Proxyfan.Domain.Traffic;
using System;
using System.Buffers;
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
///     Header values are copied verbatim as raw bytes; only header names and synthesized lines
///     (request line, <c>Via</c>, <c>Content-Length</c>) are parsed or emitted as ASCII. This
///     preserves any obs-text bytes (RFC 7230 § 3.2.6) present in cookies, signed headers, or
///     custom metadata that would otherwise be mangled by an ASCII decode/encode round trip.
/// </summary>
public static class OriginRequestRewriter
{
    private const string ViaToken = "1.1 proxyfan";
    private static readonly HashSet<string> AlwaysStrippedHeaders;
    private static readonly byte[] LineTerminator;

    static OriginRequestRewriter()
    {
        var alwaysStripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Connection",
            "Content-Length",
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
            "Transfer-Encoding",
        };
        AlwaysStrippedHeaders = alwaysStripped;
        var lineTerminator = new byte[]
        {
            (byte)'\r',
            (byte)'\n',
        };
        LineTerminator = lineTerminator;
    }

    /// <summary>
    ///     Returns the rewritten header bytes for forwarding directly to an origin server. The
    ///     body bytes are unchanged and should be written after the returned header bytes. The
    ///     rewriter normalizes body framing: <c>Transfer-Encoding</c> and <c>Content-Length</c>
    ///     are stripped from the inbound headers; when the request carries a decoded body, a
    ///     fresh <c>Content-Length</c> matching the body length is injected (chunked-decoded
    ///     bodies must not be re-emitted under chunked framing).
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data (used for method, URI, version, body length).</param>
    /// <returns>The rewritten header bytes ending with the CRLF CRLF terminator.</returns>
    public static byte[] RewriteHeaders(ReadOnlyMemory<byte> originalHeaderBytes, HypertextTransferProtocolRequestData request)
    {
        var span = originalHeaderBytes.Span;
        var firstLineEnd = FindLineTerminatorIndex(span, 0);

        if (firstLineEnd < 0)
        {
            return originalHeaderBytes.ToArray();
        }

        var originForm = BuildOriginForm(request);
        var rewrittenRequestLine = $"{request.Method} {originForm} {request.Version}";

        var headerSectionStart = firstLineEnd + 2;
        var headerSection = originalHeaderBytes[headerSectionStart..];
        var headerLines = SplitHeaderLines(headerSection.Span);
        var connectionListedNames = ExtractConnectionListedHeaderNames(headerLines, headerSection.Span);

        var output = new ArrayBufferWriter<byte>(originalHeaderBytes.Length + ViaToken.Length + 64);
        AppendAscii(output, rewrittenRequestLine);
        AppendLineTerminator(output);

        var viaAppendedInline = HasAppendedHeadersWithInlineVia(output, headerSection.Span, headerLines, connectionListedNames);

        if (!viaAppendedInline)
        {
            AppendAscii(output, "Via: ");
            AppendAscii(output, ViaToken);
            AppendLineTerminator(output);
        }

        if (request.Body.Length > 0)
        {
            AppendAscii(output, "Content-Length: ");
            AppendAscii(output, request.Body.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AppendLineTerminator(output);
        }

        AppendLineTerminator(output);
        return output.WrittenSpan.ToArray();
    }

    private static void AppendAscii(ArrayBufferWriter<byte> output, string value)
    {
        var byteCount = Encoding.ASCII.GetByteCount(value);
        var destination = output.GetSpan(byteCount);
        Encoding.ASCII.GetBytes(value, destination);
        output.Advance(byteCount);
    }

    private static void AppendLineTerminator(ArrayBufferWriter<byte> output)
    {
        output.Write(LineTerminator);
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

    private static string DecodeTrimmedAscii(ReadOnlySpan<byte> span)
    {
        var start = 0;
        var end = span.Length;

        while (start < end && HasOptionalWhitespace(span[start]))
        {
            start++;
        }

        while (end > start && HasOptionalWhitespace(span[end - 1]))
        {
            end--;
        }

        return Encoding.ASCII.GetString(span[start..end]);
    }

    private static HashSet<string> ExtractConnectionListedHeaderNames(
        IReadOnlyList<HeaderLineRange> headerLines,
        ReadOnlySpan<byte> headerSectionSpan)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var range in headerLines)
        {
            if (range.Length == 0)
            {
                continue;
            }

            var lineSpan = headerSectionSpan.Slice(range.Offset, range.Length);
            var colonIndex = lineSpan.IndexOf((byte)':');

            if (colonIndex <= 0)
            {
                continue;
            }

            var name = DecodeTrimmedAscii(lineSpan[..colonIndex]);

            if (!string.Equals(name, "Connection", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = DecodeTrimmedAscii(lineSpan[(colonIndex + 1)..]);
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

    private static int FindLineTerminatorIndex(ReadOnlySpan<byte> span, int start)
    {
        for (var index = start; index + 1 < span.Length; index++)
        {
            if (span[index] == (byte)'\r' && span[index + 1] == (byte)'\n')
            {
                return index;
            }
        }

        return -1;
    }

    private static bool HasAppendedHeadersWithInlineVia(
        ArrayBufferWriter<byte> output,
        ReadOnlySpan<byte> headerSectionSpan,
        IReadOnlyList<HeaderLineRange> headerLines,
        HashSet<string> connectionListedNames)
    {
        var viaAppendedInline = false;

        foreach (var range in headerLines)
        {
            if (range.Length == 0)
            {
                continue;
            }

            var lineSpan = headerSectionSpan.Slice(range.Offset, range.Length);
            var colonIndex = lineSpan.IndexOf((byte)':');

            if (colonIndex <= 0)
            {
                output.Write(lineSpan);
                AppendLineTerminator(output);
                continue;
            }

            var name = DecodeTrimmedAscii(lineSpan[..colonIndex]);

            if (AlwaysStrippedHeaders.Contains(name) || connectionListedNames.Contains(name))
            {
                continue;
            }

            if (string.Equals(name, "Via", StringComparison.OrdinalIgnoreCase))
            {
                var trimmed = TrimTrailingWhitespace(lineSpan);
                output.Write(trimmed);
                AppendAscii(output, ", ");
                AppendAscii(output, ViaToken);
                AppendLineTerminator(output);
                viaAppendedInline = true;
                continue;
            }

            output.Write(lineSpan);
            AppendLineTerminator(output);
        }

        return viaAppendedInline;
    }

    private static bool HasOptionalWhitespace(byte value)
    {
        return value is (byte)' ' or (byte)'\t';
    }

    private static List<HeaderLineRange> SplitHeaderLines(ReadOnlySpan<byte> headerSectionSpan)
    {
        var result = new List<HeaderLineRange>();
        var cursor = 0;

        while (cursor < headerSectionSpan.Length)
        {
            var next = FindLineTerminatorIndex(headerSectionSpan, cursor);

            if (next < 0)
            {
                var tail = new HeaderLineRange(cursor, headerSectionSpan.Length - cursor);
                result.Add(tail);
                break;
            }

            var range = new HeaderLineRange(cursor, next - cursor);
            result.Add(range);
            cursor = next + 2;
        }

        return result;
    }

    private static ReadOnlySpan<byte> TrimTrailingWhitespace(ReadOnlySpan<byte> span)
    {
        var end = span.Length;

        while (end > 0 && HasOptionalWhitespace(span[end - 1]))
        {
            end--;
        }

        return span[..end];
    }

    private readonly struct HeaderLineRange
    {
        public int Length { get; }

        public int Offset { get; }

        public HeaderLineRange(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }
    }
}
