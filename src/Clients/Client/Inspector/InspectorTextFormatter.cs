using Proxyfan.Domain.Traffic;
using System;
using System.Text;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Provides static helper methods for formatting traffic request and response data as display text.
/// </summary>
public static class InspectorTextFormatter
{
    /// <summary>
    ///     Renders a body for display by applying Content-Encoding decompression, charset
    ///     decoding, and media-type-aware pretty-printing.
    /// </summary>
    /// <param name="body">
    ///     The raw body bytes.
    /// </param>
    /// <param name="headers">
    ///     The headers used to determine Content-Type and Content-Encoding.
    /// </param>
    /// <returns>
    ///     The decoded, decoded-and-pretty-printed display text, or an empty string when the body is empty.
    /// </returns>
    public static string FormatBody(ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        return InspectorBodyRenderer.Render(body, headers);
    }

    /// <summary>
    ///     Decodes a body byte buffer to a UTF-8 display string without applying
    ///     Content-Encoding or media-type-aware formatting. Provided for callers without
    ///     a header collection.
    /// </summary>
    /// <param name="body">
    ///     The raw body bytes to decode.
    /// </param>
    /// <returns>
    ///     The decoded text, or an empty string when the body is empty.
    /// </returns>
    public static string FormatBody(ReadOnlyMemory<byte> body)
    {
        if (body.IsEmpty)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(body.Span);
    }

    /// <summary>
    ///     Formats a header collection as a multi-line name: value string.
    /// </summary>
    /// <param name="headers">
    ///     The headers to format.
    /// </param>
    /// <returns>
    ///     A string with each header on its own line.
    /// </returns>
    public static string FormatHeaders(HeaderCollection headers)
    {
        var builder = new StringBuilder();

        foreach (var header in headers)
        {
            foreach (var value in header.Value)
            {
                builder.Append(header.Key);
                builder.Append(": ");
                builder.AppendLine(value);
            }
        }

        return builder.ToString();
    }
}