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
    ///     Decodes a body byte buffer to a UTF-8 display string.
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