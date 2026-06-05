using Proxyfan.Framework.Serialization;
using System;

namespace Proxyfan.Client.Tools;

/// <summary>
///     Wraps framework serialization primitives behind a client-owned façade.
/// </summary>
public static class InspectorSerializationFormatter
{
    /// <summary>
    ///     Gets the default maximum decompressed byte count.
    /// </summary>
    public static long DefaultMaxDecompressedBytes => ContentEncodingDecoder.DefaultMaxDecompressedBytes;

    /// <summary>
    ///     Gets the default maximum decompression ratio.
    /// </summary>
    public static double DefaultMaxDecompressionRatio => ContentEncodingDecoder.DefaultMaxDecompressionRatio;

    /// <summary>
    ///     Decodes content-encoded payload bytes.
    /// </summary>
    /// <param name="contentEncoding">The Content-Encoding header value.</param>
    /// <param name="payload">The payload bytes to decode.</param>
    /// <param name="maxDecompressedBytes">The decompressed-size safety limit.</param>
    /// <param name="maxDecompressionRatio">The decompression-ratio safety limit.</param>
    /// <returns>The decoded payload bytes.</returns>
    public static byte[] DecodeContentEncoding(string contentEncoding, byte[] payload, long maxDecompressedBytes, double maxDecompressionRatio)
    {
        return ContentEncodingDecoder.Decode(contentEncoding, payload, maxDecompressedBytes, maxDecompressionRatio);
    }

    /// <summary>
    ///     Formats bytes as a hex dump.
    /// </summary>
    /// <param name="payload">The payload bytes.</param>
    /// <returns>The hex-dump text.</returns>
    public static string FormatHexDump(byte[] payload)
    {
        return HexDumpFormatter.Format(payload);
    }

    /// <summary>
    ///     Pretty-prints URL-encoded form text.
    /// </summary>
    /// <param name="text">The URL-encoded form text.</param>
    /// <returns>The pretty-printed key/value text.</returns>
    public static string PrettyPrintFormUrlEncoded(string text)
    {
        return FormUrlEncodedPrettyPrinter.PrettyPrint(text);
    }

    /// <summary>
    ///     Pretty-prints JSON text.
    /// </summary>
    /// <param name="text">The JSON text.</param>
    /// <returns>The pretty-printed JSON text.</returns>
    public static string PrettyPrintJson(string text)
    {
        return JsonPrettyPrinter.PrettyPrint(text);
    }

    /// <summary>
    ///     Pretty-prints protobuf payload bytes without schema metadata.
    /// </summary>
    /// <param name="payload">The protobuf payload bytes.</param>
    /// <returns>The pretty-printed protobuf text.</returns>
    public static string PrettyPrintProtobuf(ReadOnlyMemory<byte> payload)
    {
        return ProtobufPrettyPrinter.PrettyPrint(payload);
    }

    /// <summary>
    ///     Pretty-prints protobuf payload bytes using schema metadata.
    /// </summary>
    /// <param name="payload">The protobuf payload bytes.</param>
    /// <param name="schemaToken">An opaque schema token.</param>
    /// <param name="indexToken">An opaque descriptor-index token.</param>
    /// <returns>The pretty-printed protobuf text.</returns>
    public static string PrettyPrintProtobufSchemaAware(ReadOnlyMemory<byte> payload, object schemaToken, object indexToken)
    {
        if (schemaToken is not ProtobufMessageDescriptor schema)
        {
            throw new ArgumentException("Invalid schema token.", nameof(schemaToken));
        }

        if (indexToken is not ProtobufDescriptorIndex index)
        {
            throw new ArgumentException("Invalid descriptor index token.", nameof(indexToken));
        }

        return ProtobufSchemaAwarePrettyPrinter.PrettyPrint(payload, schema, index);
    }

    /// <summary>
    ///     Pretty-prints XML text.
    /// </summary>
    /// <param name="text">The XML text.</param>
    /// <returns>The pretty-printed XML text.</returns>
    public static string PrettyPrintXml(string text)
    {
        return XmlPrettyPrinter.PrettyPrint(text);
    }
}
