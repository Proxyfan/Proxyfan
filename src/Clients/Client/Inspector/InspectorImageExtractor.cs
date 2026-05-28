using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using System;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Extracts decompressed image bytes from an HTTP message body when the message
///     declares an <c>image/*</c> Content-Type. Returns <see langword="null" /> for non-image
///     bodies so the Inspector can fall back to the textual hex-dump renderer.
/// </summary>
public static class InspectorImageExtractor
{
    /// <summary>
    ///     Returns the decompressed image bytes if the body is an image; otherwise
    ///     <see langword="null" />.
    /// </summary>
    /// <param name="body">The raw body bytes.</param>
    /// <param name="headers">The message headers (used for Content-Type and Content-Encoding).</param>
    /// <returns>The decoded image bytes, or <see langword="null" /> when not an image.</returns>
    public static byte[]? TryExtract(ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        if (body.IsEmpty)
        {
            return null;
        }

        var contentType = ParseContentType(headers);

        if (contentType is null)
        {
            return null;
        }

        if (!contentType.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return DecodeContentEncoding(body, headers);
    }

    private static byte[] DecodeContentEncoding(ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        var contentEncoding = headers.Get("Content-Encoding");

        if (string.IsNullOrWhiteSpace(contentEncoding))
        {
            return body.ToArray();
        }

        try
        {
            return ContentEncodingDecoder.Decode(contentEncoding, body.ToArray());
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

    private static ContentType? ParseContentType(HeaderCollection headers)
    {
        var raw = headers.Get("Content-Type");

        if (raw is null)
        {
            return null;
        }

        return ContentTypeParser.Parse(raw);
    }
}
