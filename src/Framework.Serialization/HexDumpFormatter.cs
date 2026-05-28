using System;
using System.Text;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Formats binary data as a classic hex+ASCII dump. Each line shows the byte offset,
///     up to 16 hexadecimal bytes, and an ASCII representation where printable characters
///     are shown verbatim and non-printable bytes are replaced with a dot.
/// </summary>
public static class HexDumpFormatter
{
    private const int BytesPerLine = 16;
    private const int HexGroupSize = 8;
    private const int OffsetDigits = 8;

    /// <summary>
    ///     Renders the supplied bytes as a hex dump.
    /// </summary>
    /// <param name="bytes">The bytes to render.</param>
    /// <returns>The formatted hex dump, or an empty string when the input is empty.</returns>
    public static string Format(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var totalLines = (bytes.Length + BytesPerLine - 1) / BytesPerLine;

        for (var lineIndex = 0; lineIndex < totalLines; lineIndex++)
        {
            var offset = lineIndex * BytesPerLine;
            var lineLength = Math.Min(BytesPerLine, bytes.Length - offset);
            AppendLine(builder, bytes.Slice(offset, lineLength), offset);

            if (lineIndex < totalLines - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static void AppendAscii(StringBuilder builder, ReadOnlySpan<byte> line)
    {
        foreach (var value in line)
        {
            builder.Append(HasPrintableAscii(value) ? (char)value : '.');
        }
    }

    private static void AppendHex(StringBuilder builder, ReadOnlySpan<byte> line)
    {
        for (var index = 0; index < BytesPerLine; index++)
        {
            if (index < line.Length)
            {
                builder.Append(line[index].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append("  ");
            }

            if (index == HexGroupSize - 1)
            {
                builder.Append("  ");
            }
            else if (index < BytesPerLine - 1)
            {
                builder.Append(' ');
            }
        }
    }

    private static void AppendLine(StringBuilder builder, ReadOnlySpan<byte> line, int offset)
    {
        builder.Append(offset.ToString("x" + OffsetDigits.ToString(System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture));
        builder.Append("  ");
        AppendHex(builder, line);
        builder.Append("  ");
        AppendAscii(builder, line);
    }

    private static bool HasPrintableAscii(byte value)
    {
        return value is >= 0x20 and < 0x7F;
    }
}
