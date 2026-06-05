using Proxyfan.Client.Tools;
using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;
using System.Text;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Renders an HTTP message body for display in the Inspector. Applies
///     <see cref="InspectorSerializationFormatter" /> using the Content-Encoding header, decodes
///     bytes to text using the charset (or UTF-8), then dispatches to a content-specific
///     pretty-printer based on the media type. Binary content (including images) is
///     rendered as a hex dump prefixed with a metadata header.
/// </summary>
public static class InspectorBodyRenderer
{
    static InspectorBodyRenderer()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    ///     Renders the body for display, using the default decompression safety limits.
    /// </summary>
    /// <param name="body">The raw body bytes.</param>
    /// <param name="headers">The message headers (used for Content-Type and Content-Encoding).</param>
    /// <returns>The display text.</returns>
    /// <exception cref="Proxyfan.Framework.Serialization.DecompressionLimitExceededException">
    ///     Thrown when the decompressed output exceeds the safety limits in
    ///     <see cref="InspectorSerializationFormatter" />.
    /// </exception>
    public static string Render(ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        return Render(body, headers, forceDecodeBody: false);
    }

    /// <summary>
    ///     Renders the body for display.
    /// </summary>
    /// <param name="body">The raw body bytes.</param>
    /// <param name="headers">The message headers (used for Content-Type and Content-Encoding).</param>
    /// <param name="forceDecodeBody">
    ///     When <see langword="true" />, bypasses the decompression-size and ratio limits so the
    ///     full body is always decoded. Use only when the user has explicitly requested it.
    /// </param>
    /// <returns>The display text.</returns>
    /// <exception cref="Proxyfan.Framework.Serialization.DecompressionLimitExceededException">
    ///     Thrown when <paramref name="forceDecodeBody" /> is <see langword="false" /> and the
    ///     decompressed output exceeds the safety limits in <see cref="InspectorSerializationFormatter" />.
    /// </exception>
    public static string Render(ReadOnlyMemory<byte> body, HeaderCollection headers, bool forceDecodeBody)
    {
        if (body.IsEmpty)
        {
            return string.Empty;
        }

        var decoded = DecodeContentEncoding(body, headers, forceDecodeBody);
        var contentType = ParseContentType(headers);

        if (HasProtobufMediaType(contentType))
        {
            return InspectorSerializationFormatter.PrettyPrintProtobuf(decoded);
        }

        if (HasImageMediaType(contentType))
        {
            return RenderImage(decoded, contentType);
        }

        if (HasBinaryMediaType(contentType))
        {
            return RenderBinary(decoded, contentType);
        }

        if (HasFormUrlEncodedMediaType(contentType))
        {
            var charset = ResolveCharset(contentType);
            var formText = DecodeText(decoded, charset);
            return InspectorSerializationFormatter.PrettyPrintFormUrlEncoded(formText);
        }

        var resolvedCharset = ResolveCharset(contentType);
        var text = DecodeText(decoded, resolvedCharset);
        return PrettyPrintByMediaType(text, contentType);
    }

    private static byte[] DecodeContentEncoding(ReadOnlyMemory<byte> body, HeaderCollection headers, bool forceDecodeBody)
    {
        var contentEncoding = headers.Get("Content-Encoding");

        if (string.IsNullOrWhiteSpace(contentEncoding))
        {
            return body.ToArray();
        }

        try
        {
            var maxDecompressedBytes = forceDecodeBody ? long.MaxValue : InspectorSerializationFormatter.DefaultMaxDecompressedBytes;
            var maxDecompressionRatio = forceDecodeBody ? double.MaxValue : InspectorSerializationFormatter.DefaultMaxDecompressionRatio;
            return InspectorSerializationFormatter.DecodeContentEncoding(contentEncoding, body.ToArray(), maxDecompressedBytes, maxDecompressionRatio);
        }
        catch (NotSupportedException)
        {
            return body.ToArray();
        }
        catch (InvalidOperationException)
        {
            return body.ToArray();
        }
        catch (System.IO.InvalidDataException)
        {
            return body.ToArray();
        }
    }

    private static string DecodeText(byte[] bytes, Encoding charset)
    {
        try
        {
            return charset.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }

    private static bool HasBinaryMediaType(ContentType? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        var mediaType = contentType.MediaType;

        if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.StartsWith("font/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/zip", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/gzip", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/wasm", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool HasFormUrlEncodedMediaType(ContentType? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        return contentType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasHypertextMarkupLanguageMediaType(string mediaType)
    {
        if (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool HasImageMediaType(ContentType? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        return contentType.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasJsonMediaType(string mediaType)
    {
        if (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool HasProtobufMediaType(ContentType? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        var mediaType = contentType.MediaType;

        if (mediaType.Equals("application/protobuf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/x-protobuf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.EndsWith("+protobuf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool HasXmlMediaType(string mediaType)
    {
        if (mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("text/xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static ContentType? ParseContentType(HeaderCollection headers)
    {
        var raw = headers.Get("Content-Type");

        if (raw is null)
        {
            return null;
        }

        return ContentTypeParser.Parse(raw);
    }

    private static string PrettyPrintByMediaType(string text, ContentType? contentType)
    {
        if (contentType is null)
        {
            return text;
        }

        var mediaType = contentType.MediaType;

        if (HasJsonMediaType(mediaType))
        {
            return InspectorSerializationFormatter.PrettyPrintJson(text);
        }

        if (HasHypertextMarkupLanguageMediaType(mediaType))
        {
            return InspectorSerializationFormatter.PrettyPrintXml(text);
        }

        if (HasXmlMediaType(mediaType))
        {
            return InspectorSerializationFormatter.PrettyPrintXml(text);
        }

        return text;
    }

    private static string RenderBinary(byte[] decoded, ContentType? contentType)
    {
        var builder = new StringBuilder();
        builder.Append("[Binary: ");
        builder.Append(contentType?.MediaType ?? "application/octet-stream");
        builder.Append(", ");
        builder.Append(decoded.Length.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine(" bytes]");
        builder.Append(InspectorSerializationFormatter.FormatHexDump(decoded));
        return builder.ToString();
    }

    private static string RenderImage(byte[] decoded, ContentType? contentType)
    {
        var builder = new StringBuilder();
        builder.Append("[Image: ");
        builder.Append(contentType?.MediaType ?? "image/unknown");
        builder.Append(", ");
        builder.Append(decoded.Length.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine(" bytes]");
        builder.Append(InspectorSerializationFormatter.FormatHexDump(decoded));
        return builder.ToString();
    }

    private static Encoding ResolveCharset(ContentType? contentType)
    {
        if (contentType?.Charset is null)
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(contentType.Charset);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }
}
