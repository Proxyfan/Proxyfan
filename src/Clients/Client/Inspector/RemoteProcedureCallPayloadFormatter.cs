using Proxyfan.Client.Tools;
using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;
using System.Text;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Static helpers that format <see cref="RemoteProcedureCallCapturedMessage" /> payloads
///     for the gRPC inspector tab. Uncompressed payloads are rendered as a schema-less
///     protobuf field tree (via <see cref="InspectorSerializationFormatter" />) followed by a hex+ASCII
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
    ///     the schema-less decoder (no field names).
    /// </summary>
    /// <param name="capturedMessage">The captured gRPC message to render.</param>
    /// <returns>A multi-line human-readable rendering.</returns>
    public static string FormatFull(RemoteProcedureCallCapturedMessage capturedMessage)
    {
        return FormatFull(capturedMessage, schemaResolution: null);
    }

    /// <summary>
    ///     Returns a verbose multi-line rendering of <paramref name="capturedMessage" /> showing
    ///     direction, compression flag, capture timestamp, payload byte count, a decoded
    ///     protobuf field tree (when the payload is uncompressed and parses cleanly), and a
    ///     hex+ASCII dump of the raw payload.
    /// </summary>
    /// <param name="capturedMessage">The captured gRPC message to render.</param>
    /// <param name="schemaResolution">Optional schema metadata for schema-aware decoding.</param>
    /// <returns>A multi-line human-readable rendering.</returns>
    public static string FormatFull(
        RemoteProcedureCallCapturedMessage capturedMessage,
        RemoteProcedureCallSchemaResolution? schemaResolution)
    {
        var builder = new StringBuilder();
        builder.Append("Captured  : ");
        builder.AppendLine(capturedMessage.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        builder.Append("Direction : ");
        builder.AppendLine(DirectionLabel(capturedMessage.Direction));
        builder.Append("Compressed: ");
        builder.AppendLine(capturedMessage.IsCompressed ? "yes" : "no");
        if (!string.IsNullOrEmpty(schemaResolution?.SchemaFullName))
        {
            builder.Append("Schema    : ");
            builder.AppendLine(schemaResolution.SchemaFullName);
        }

        builder.Append("Length    : ");
        builder.Append(capturedMessage.Payload.Length.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine(" bytes");
        builder.AppendLine();
        AppendDecodedPayload(builder, capturedMessage, schemaResolution);
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

    private static void AppendDecodedPayload(StringBuilder builder, RemoteProcedureCallCapturedMessage capturedMessage, RemoteProcedureCallSchemaResolution? schemaResolution)
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
        if (schemaResolution?.SchemaToken is not null && schemaResolution.IndexToken is not null)
        {
            prettyPrinted = InspectorSerializationFormatter.PrettyPrintProtobufSchemaAware(
                capturedMessage.Payload,
                schemaResolution.SchemaToken,
                schemaResolution.IndexToken);
            heading = "Decoded protobuf (schema):";
        }
        else
        {
            prettyPrinted = InspectorSerializationFormatter.PrettyPrintProtobuf(capturedMessage.Payload);
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
