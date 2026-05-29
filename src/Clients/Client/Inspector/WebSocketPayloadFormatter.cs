using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Provides formatting helpers for <see cref="WebSocketMessage" /> instances. Text
///     payloads are decoded as UTF-8; binary payloads are rendered as a hex dump.
///     JSON-looking text payloads are pretty-printed; malformed JSON falls back to the
///     raw decoded text.
/// </summary>
public static class WebSocketPayloadFormatter
{
    private const int PreviewMaximumCharacters = 80;

    /// <summary>
    ///     Renders the full payload of the supplied message for display in the detail
    ///     panel. Text frames return decoded text (pretty-printed when JSON), binary
    ///     frames return a 16-column hex dump, and control frames (Close/Ping/Pong)
    ///     return the opcode label followed by a hex dump of any payload bytes.
    /// </summary>
    /// <param name="message">The message to format.</param>
    /// <returns>The formatted payload text, or an empty string when the payload is empty.</returns>
    public static string FormatFull(WebSocketMessage message)
    {
        if (message is null)
        {
            return string.Empty;
        }

        var payload = message.Payload;
        if (payload.IsEmpty)
        {
            return string.Empty;
        }

        if (message.Opcode == WebSocketOpcode.Text)
        {
            var text = DecodeText(payload);
            return TryPrettyPrintJson(text);
        }

        if (message.Opcode == WebSocketOpcode.Binary)
        {
            return FormatHexDump(payload);
        }

        return FormatHexDump(payload);
    }

    /// <summary>
    ///     Renders a one-line preview of the supplied message for display in the
    ///     message list. Text payloads are truncated and have CR/LF replaced with
    ///     a visible glyph; binary payloads show the byte count.
    /// </summary>
    /// <param name="message">The message to summarise.</param>
    /// <returns>A short preview string.</returns>
    public static string FormatPreview(WebSocketMessage message)
    {
        if (message is null)
        {
            return string.Empty;
        }

        var payload = message.Payload;

        if (message.Opcode == WebSocketOpcode.Text)
        {
            var text = DecodeText(payload);
            return Truncate(EscapeWhitespace(text), PreviewMaximumCharacters);
        }

        if (message.Opcode == WebSocketOpcode.Binary)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "<{0} byte{1}>",
                payload.Length,
                payload.Length == 1 ? string.Empty : "s");
        }

        if (payload.IsEmpty)
        {
            return string.Empty;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "<{0} byte{1}>",
            payload.Length,
            payload.Length == 1 ? string.Empty : "s");
    }

    private static void AppendHexDumpAsciiSegment(StringBuilder builder, ReadOnlySpan<byte> span, int offset)
    {
        for (var column = 0; column < 16; column++)
        {
            var index = offset + column;
            if (index >= span.Length)
            {
                return;
            }

            var value = span[index];
            builder.Append(value is >= 0x20 and < 0x7F ? (char)value : '.');
        }
    }

    private static void AppendHexDumpHexSegment(StringBuilder builder, ReadOnlySpan<byte> span, int offset)
    {
        for (var column = 0; column < 16; column++)
        {
            var index = offset + column;
            if (index < span.Length)
            {
                builder.Append(span[index].ToString("X2", CultureInfo.InvariantCulture));
                builder.Append(' ');
            }
            else
            {
                builder.Append("   ");
            }

            if (column == 7)
            {
                builder.Append(' ');
            }
        }
    }

    private static string DecodeText(ReadOnlyMemory<byte> payload)
    {
        try
        {
            return Encoding.UTF8.GetString(payload.Span);
        }
        catch (DecoderFallbackException)
        {
            return FormatHexDump(payload);
        }
    }

    private static string EscapeWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (character == '\r')
            {
                builder.Append('␍');
                continue;
            }

            if (character == '\n')
            {
                builder.Append('␊');
                continue;
            }

            if (character == '\t')
            {
                builder.Append('␉');
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static string FormatHexDump(ReadOnlyMemory<byte> payload)
    {
        var span = payload.Span;
        var builder = new StringBuilder(payload.Length * 4);
        for (var offset = 0; offset < span.Length; offset += 16)
        {
            builder.Append(offset.ToString("X8", CultureInfo.InvariantCulture));
            builder.Append("  ");
            AppendHexDumpHexSegment(builder, span, offset);
            builder.Append(' ');
            AppendHexDumpAsciiSegment(builder, span, offset);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string Truncate(string text, int maximumCharacters)
    {
        if (text.Length <= maximumCharacters)
        {
            return text;
        }

        return text.AsSpan(0, maximumCharacters).ToString() + "…";
    }

    private static string TryPrettyPrintJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var trimmed = text.TrimStart();
        if (trimmed.Length == 0)
        {
            return text;
        }

        var first = trimmed[0];
        if (first is not '{' and not '[')
        {
            return text;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            using var stream = new System.IO.MemoryStream();
            var writerOptions = new JsonWriterOptions
            {
                Indented = true,
            };
            using (var writer = new Utf8JsonWriter(stream, writerOptions))
            {
                document.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return text;
        }
    }
}
