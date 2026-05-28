using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using System;
using System.Text;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Renders an HTTP message body for display in the Inspector. Applies
///     <see cref="ContentEncodingDecoder" /> using the Content-Encoding header, decodes
///     bytes to text using the charset (or UTF-8), then pretty-prints when the media type
///     indicates JSON or XML.
/// </summary>
public static class InspectorBodyRenderer
{
    static InspectorBodyRenderer()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    ///     Renders the body for display.
    /// </summary>
    /// <param name="body">The raw body bytes.</param>
    /// <param name="headers">The message headers (used for Content-Type and Content-Encoding).</param>
    /// <returns>The display text.</returns>
    public static string Render(ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        if (body.IsEmpty)
        {
            return string.Empty;
        }

        var decoded = DecodeContentEncoding(body, headers);
        var contentType = ParseContentType(headers);
        var charset = ResolveCharset(contentType);
        var text = DecodeText(decoded, charset);
        return PrettyPrintByMediaType(text, contentType);
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
            return JsonPrettyPrinter.PrettyPrint(text);
        }

        if (HasXmlMediaType(mediaType))
        {
            return XmlPrettyPrinter.PrettyPrint(text);
        }

        return text;
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
