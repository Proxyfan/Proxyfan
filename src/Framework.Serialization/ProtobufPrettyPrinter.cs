using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Pretty-prints a raw protobuf payload as a human-readable indented tree of fields. When
///     a length-delimited field's value parses as a valid nested protobuf message, the
///     printer recurses; otherwise it tries UTF-8 (text bytes only) and falls back to hex.
/// </summary>
public static class ProtobufPrettyPrinter
{
    /// <summary>
    ///     Returns a pretty-printed representation of the supplied protobuf payload, or the
    ///     payload rendered as hex bytes when decoding fails.
    /// </summary>
    /// <param name="payload">The encoded protobuf bytes.</param>
    /// <returns>The human-readable tree representation.</returns>
    public static string PrettyPrint(ReadOnlyMemory<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return string.Empty;
        }

        try
        {
            var fields = ProtobufDecoder.Decode(payload);
            var builder = new StringBuilder();
            WriteFields(builder, fields, indentLevel: 0);
            return builder.ToString().TrimEnd();
        }
        catch (InvalidDataException)
        {
            return RenderHex(payload.Span);
        }
    }

    private static void AppendIndent(StringBuilder builder, int indentLevel)
    {
        for (var index = 0; index < indentLevel; index++)
        {
            builder.Append("  ");
        }
    }

    private static void AppendUnknownField(StringBuilder builder, ProtobufField field, int indentLevel)
    {
        AppendIndent(builder, indentLevel);
        builder.Append("Field ");
        builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
        builder.Append(" (unknown wire type ");
        builder.Append(((int)field.WireType).ToString(CultureInfo.InvariantCulture));
        builder.Append(')');
        builder.Append('\n');
    }

    private static bool CanRenderAsText(ReadOnlySpan<byte> bytes, out string text)
    {
        text = string.Empty;
        if (bytes.IsEmpty)
        {
            return false;
        }

        try
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var decoded = utf8.GetString(bytes);
            foreach (var character in decoded)
            {
                if (character is '\t' or '\r' or '\n')
                {
                    continue;
                }

                if (character is < (char)0x20 or (char)0x7F)
                {
                    return false;
                }
            }

            text = decoded;
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool HasNestedMessage(byte[] bytes, out IReadOnlyList<ProtobufField> nested)
    {
        nested = [];
        if (bytes.Length == 0)
        {
            return false;
        }

        try
        {
            nested = ProtobufDecoder.Decode(bytes);
            return nested.Count > 0;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static string RenderHex(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
        {
            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static void WriteFields(StringBuilder builder, IReadOnlyList<ProtobufField> fields, int indentLevel)
    {
        foreach (var field in fields)
        {
            WriteSingleField(builder, field, indentLevel);
        }
    }

    private static void WriteLengthDelimitedField(StringBuilder builder, ProtobufField field, int indentLevel)
    {
        if (field.Value is not byte[] bytes)
        {
            return;
        }

        if (CanRenderAsText(bytes, out var text))
        {
            AppendIndent(builder, indentLevel);
            builder.Append("Field ");
            builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
            builder.Append(" (string): \"");
            builder.Append(text);
            builder.Append('"');
            builder.Append('\n');
            return;
        }

        if (HasNestedMessage(bytes, out var nested))
        {
            AppendIndent(builder, indentLevel);
            builder.Append("Field ");
            builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
            builder.Append(" (message): {");
            builder.Append('\n');
            WriteFields(builder, nested, indentLevel + 1);
            AppendIndent(builder, indentLevel);
            builder.Append('}');
            builder.Append('\n');
            return;
        }

        AppendIndent(builder, indentLevel);
        builder.Append("Field ");
        builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
        builder.Append(" (bytes, ");
        builder.Append(bytes.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append("): 0x");
        builder.Append(RenderHex(bytes));
        builder.Append('\n');
    }

    private static void WriteSingleField(StringBuilder builder, ProtobufField field, int indentLevel)
    {
        switch (field.WireType)
        {
            case ProtobufWireType.Varint:
                AppendIndent(builder, indentLevel);
                builder.Append("Field ");
                builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
                builder.Append(" (varint): ");
                builder.Append(((ulong)field.Value).ToString(CultureInfo.InvariantCulture));
                builder.Append('\n');
                break;
            case ProtobufWireType.I64:
                AppendIndent(builder, indentLevel);
                builder.Append("Field ");
                builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
                builder.Append(" (fixed64): ");
                builder.Append(((ulong)field.Value).ToString(CultureInfo.InvariantCulture));
                builder.Append('\n');
                break;
            case ProtobufWireType.I32:
                AppendIndent(builder, indentLevel);
                builder.Append("Field ");
                builder.Append(field.FieldNumber.ToString(CultureInfo.InvariantCulture));
                builder.Append(" (fixed32): ");
                builder.Append(((uint)field.Value).ToString(CultureInfo.InvariantCulture));
                builder.Append('\n');
                break;
            case ProtobufWireType.LengthDelimited:
                WriteLengthDelimitedField(builder, field, indentLevel);
                break;
            case ProtobufWireType.StartGroup:
                AppendUnknownField(builder, field, indentLevel);
                break;
            case ProtobufWireType.EndGroup:
                AppendUnknownField(builder, field, indentLevel);
                break;
            default:
                AppendUnknownField(builder, field, indentLevel);
                break;
        }
    }
}
