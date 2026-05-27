using Proxyfan.Domain.Traffic;
using System;
using System.Text;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Static helpers that convert between the binary on-the-wire HTTP message representation
///     used by the proxy pipeline and the editable text representation surfaced by the
///     Breakpoint UI.
/// </summary>
public static class BreakpointMessageTextHelpers
{
    /// <summary>
    ///     Decodes the supplied UTF-8 body bytes into a string. Returns the empty string when the
    ///     body is empty.
    /// </summary>
    /// <param name="body">The body bytes to decode.</param>
    /// <returns>The decoded UTF-8 string.</returns>
    public static string DecodeBody(ReadOnlyMemory<byte> body)
    {
        if (body.Length == 0)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(body.Span);
    }

    /// <summary>
    ///     Encodes the supplied string as UTF-8 body bytes. Returns an empty byte array when the
    ///     supplied text is null or empty.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>The encoded UTF-8 bytes.</returns>
    public static byte[] EncodeBody(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    ///     Formats the supplied header collection as a multi-line string with one
    ///     <c>Name: Value</c> entry per line.
    /// </summary>
    /// <param name="headers">The headers to format.</param>
    /// <returns>A newline-separated string with one header per line.</returns>
    public static string FormatHeaders(HeaderCollection headers)
    {
        var builder = new StringBuilder();
        foreach (var pair in headers)
        {
            foreach (var value in pair.Value)
            {
                builder.Append(pair.Key);
                builder.Append(": ");
                builder.Append(value);
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Parses the supplied multi-line string into a header collection. Lines without a colon
    ///     separator or with an empty name are ignored.
    /// </summary>
    /// <param name="text">The multi-line text to parse.</param>
    /// <returns>A header collection populated with the parsed entries.</returns>
    public static HeaderCollection ParseHeaders(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return HeaderCollection.Empty;
        }

        var result = HeaderCollection.Empty;
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim('\r', ' ', '\t');
            if (trimmed.Length == 0)
            {
                continue;
            }

            var separator = trimmed.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var name = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            result = result.Add(name, value);
        }

        return result;
    }
}
