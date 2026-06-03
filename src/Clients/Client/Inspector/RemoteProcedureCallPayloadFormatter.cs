using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using System;
using System.Globalization;
using System.Text;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Static helpers that format <see cref="RemoteProcedureCallCapturedMessage" /> payloads
///     for the gRPC inspector tab. Uncompressed payloads are rendered as a schema-less
///     protobuf field tree (via <see cref="ProtobufPrettyPrinter" />) followed by a hex+ASCII
///     dump for verification; compressed payloads are shown as hex only (Proxyfan does not
///     decompress gRPC frames by default). Payloads larger than
///     <c>DetailPayloadByteLimit</c> bytes have their decoded tree skipped and their hex
///     dump truncated so the inspector remains responsive on large messages.
/// </summary>
public static class RemoteProcedureCallPayloadFormatter
{
    private const int BytesPerHexLine = 16;
    private const int DetailPayloadByteLimit = 64 * 1024;
    private const int PreviewByteLimit = 24;

    /// <summary>
    ///     Returns a verbose multi-line rendering of <paramref name="capturedMessage" /> using
    ///     the schema-less decoder (no field names). Equivalent to calling the schema-aware
    ///     overload with both <paramref name="capturedMessage" /> arguments set to
    ///     <see langword="null" />.
    /// </summary>
    /// <param name="capturedMessage">The captured gRPC message to render.</param>
    /// <returns>A multi-line human-readable rendering.</returns>
    public static string FormatFull(RemoteProcedureCallCapturedMessage capturedMessage)
    {
        return FormatFull(capturedMessage, schema: null, index: null);
    }

    /// <summary>
    ///     Returns a verbose multi-line rendering of <paramref name="capturedMessage" /> showing
    ///     direction, compression flag, capture timestamp, payload byte count, a decoded
    ///     protobuf field tree (when the payload is uncompressed and parses cleanly), and a
    ///     hex+ASCII dump of the raw payload. When <paramref name="schema" /> and
    ///     <paramref name="index" /> are both supplied, the decoded tree uses the schema-aware
    ///     printer (named fields, enum value names, recursive nested messages); otherwise it
    ///     falls back to the schema-less printer.
    /// </summary>
    /// <param name="capturedMessage">The captured gRPC message to render.</param>
    /// <param name="schema">Optional message descriptor for schema-aware decoding.</param>
    /// <param name="index">Optional descriptor index for resolving nested types.</param>
    /// <returns>A multi-line human-readable rendering.</returns>
    public static string FormatFull(
        RemoteProcedureCallCapturedMessage capturedMessage,
        ProtobufMessageDescriptor? schema,
        ProtobufDescriptorIndex? index)
    {
        var builder = new StringBuilder();
        builder.Append("Captured  : ");
        builder.AppendLine(capturedMessage.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        builder.Append("Direction : ");
        builder.AppendLine(DirectionLabel(capturedMessage.Direction));
        builder.Append("Compressed: ");
        builder.AppendLine(capturedMessage.IsCompressed ? "yes" : "no");
        if (schema is not null)
        {
            builder.Append("Schema    : ");
            builder.AppendLine(schema.FullName);
        }

        builder.Append("Length    : ");
        builder.Append(capturedMessage.Payload.Length.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine(" bytes");
        builder.AppendLine();
        AppendDecodedPayload(builder, capturedMessage, schema, index);
        AppendHexDump(builder, capturedMessage.Payload.Span);
        var result = builder.ToString();
        return result;
    }

    /// <summary>
    ///     Returns a short single-line hex preview of the payload bytes (first
    ///     <c>PreviewByteLimit</c> bytes, joined with spaces, ellipsised when the payload is
    ///     larger). Used in the inspector row list.
    /// </summary>
    /// <param name="capturedMessage">The captured gRPC message to preview.</param>
    /// <returns>The single-line hex preview string.</returns>
    public static string FormatPreview(RemoteProcedureCallCapturedMessage capturedMessage)
    {
        var span = capturedMessage.Payload.Span;
        if (span.Length == 0)
        {
            return "(empty)";
        }

        var sliceLength = Math.Min(span.Length, PreviewByteLimit);
        var builder = new StringBuilder(sliceLength * 3);
        for (var index = 0; index < sliceLength; index++)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(span[index].ToString("X2", CultureInfo.InvariantCulture));
        }

        if (span.Length > PreviewByteLimit)
        {
            builder.Append(" …");
        }

        var result = builder.ToString();
        return result;
    }

    private static void AppendDecodedPayload(StringBuilder builder, RemoteProcedureCallCapturedMessage capturedMessage, ProtobufMessageDescriptor? schema, ProtobufDescriptorIndex? index)
    {
        if (capturedMessage.IsCompressed)
        {
            return;
        }

        if (capturedMessage.Payload.IsEmpty)
        {
            return;
        }

        if (capturedMessage.Payload.Length > DetailPayloadByteLimit)
        {
            builder.AppendLine("Decoded protobuf: (skipped; payload exceeds preview limit)");
            builder.AppendLine();
            builder.AppendLine("Raw bytes:");
            return;
        }

        string prettyPrinted;
        string heading;
        if (schema is not null && index is not null)
        {
            prettyPrinted = ProtobufSchemaAwarePrettyPrinter.PrettyPrint(capturedMessage.Payload, schema, index);
            heading = "Decoded protobuf (schema):";
        }
        else
        {
            prettyPrinted = ProtobufPrettyPrinter.PrettyPrint(capturedMessage.Payload);
            heading = "Decoded protobuf:";
        }

        if (string.IsNullOrEmpty(prettyPrinted))
        {
            return;
        }

        builder.AppendLine(heading);
        builder.AppendLine(prettyPrinted);
        builder.AppendLine();
        builder.AppendLine("Raw bytes:");
    }

    private static void AppendHexDump(StringBuilder builder, ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
        {
            builder.AppendLine("(empty payload)");
            return;
        }

        var dumpLength = Math.Min(payload.Length, DetailPayloadByteLimit);
        for (var offset = 0; offset < dumpLength; offset += BytesPerHexLine)
        {
            var lineLength = Math.Min(BytesPerHexLine, dumpLength - offset);
            builder.Append(offset.ToString("X8", CultureInfo.InvariantCulture));
            builder.Append("  ");

            for (var index = 0; index < BytesPerHexLine; index++)
            {
                if (index < lineLength)
                {
                    builder.Append(payload[offset + index].ToString("X2", CultureInfo.InvariantCulture));
                    builder.Append(' ');
                }
                else
                {
                    builder.Append("   ");
                }
            }

            builder.Append(' ');
            for (var index = 0; index < lineLength; index++)
            {
                var value = payload[offset + index];
                var character = value is >= 32 and < 127 ? (char)value : '.';
                builder.Append(character);
            }

            builder.AppendLine();
        }

        if (payload.Length > dumpLength)
        {
            builder.Append("… (");
            builder.Append((payload.Length - dumpLength).ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(" more bytes truncated)");
        }
    }

    private static string DirectionLabel(RemoteProcedureCallDirection direction)
    {
        if (direction == RemoteProcedureCallDirection.Outbound)
        {
            return "Outbound (client → server)";
        }

        return "Inbound (server → client)";
    }
}
