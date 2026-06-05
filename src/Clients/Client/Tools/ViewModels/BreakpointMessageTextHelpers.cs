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
    private static readonly Encoding StrictUtf8;

    static BreakpointMessageTextHelpers()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var strictUtf8 = new UTF8Encoding(false, true);
        StrictUtf8 = strictUtf8;
    }

    /// <summary>
    ///     Creates the breakpoint body editor state for the supplied message body and headers.
    ///     Textual content with a supported charset is surfaced as text; everything else is
    ///     surfaced as a reversible base64 string.
    /// </summary>
    /// <param name="body">The body bytes to decode.</param>
    /// <param name="headers">The message headers.</param>
    /// <returns>The editor state for the body.</returns>
    public static BreakpointBodyEditorState CreateBodyEditorState(ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        if (CanResolveTextEncoding(body, headers, out var encoding))
        {
            return new BreakpointBodyEditorState(encoding.GetString(body.Span), isBase64: false, encoding);
        }

        var text = body.IsEmpty ? string.Empty : Convert.ToBase64String(body.Span);
        return new BreakpointBodyEditorState(text, isBase64: true, StrictUtf8);
    }

    /// <summary>
    ///     Decodes the supplied body bytes into the breakpoint editor representation.
    /// </summary>
    /// <param name="body">The body bytes to decode.</param>
    /// <param name="headers">The message headers.</param>
    /// <returns>The decoded editor text.</returns>
    public static string DecodeBody(ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        return CreateBodyEditorState(body, headers).Text;
    }

    /// <summary>
    ///     Encodes the supplied editor text as body bytes using the same representation selected by
    ///     <see cref="CreateBodyEditorState" />. When the text is unchanged, the original byte
    ///     buffer is preserved verbatim.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="body">The original body bytes.</param>
    /// <param name="headers">The message headers.</param>
    /// <returns>The encoded body bytes.</returns>
    public static byte[] EncodeBody(string text, ReadOnlyMemory<byte> body, HeaderCollection headers)
    {
        return CreateBodyEditorState(body, headers).Encode(text, body);
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

    private static bool CanDecodeText(ReadOnlyMemory<byte> body, Encoding encoding)
    {
        try
        {
            _ = encoding.GetCharCount(body.Span);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool CanResolveDeclaredTextEncoding(ContentType? contentType, out Encoding encoding)
    {
        if (!HasTextualMediaType(contentType) || string.IsNullOrWhiteSpace(contentType!.Charset))
        {
            encoding = StrictUtf8;
            return false;
        }

        try
        {
            encoding = CreateStrictEncoding(Encoding.GetEncoding(contentType.Charset));
            return true;
        }
        catch (ArgumentException)
        {
            encoding = StrictUtf8;
            return false;
        }
    }

    private static bool CanResolveTextEncoding(ReadOnlyMemory<byte> body, HeaderCollection headers, out Encoding encoding)
    {
        var contentType = ParseContentType(headers);

        if (CanResolveDeclaredTextEncoding(contentType, out encoding) && CanDecodeText(body, encoding))
        {
            return true;
        }

        if (HasDefaultUtf8TextMediaType(contentType) && CanDecodeText(body, StrictUtf8))
        {
            encoding = StrictUtf8;
            return true;
        }

        if (CanUseUtf8TextFallback(body, contentType))
        {
            encoding = StrictUtf8;
            return true;
        }

        encoding = StrictUtf8;
        return false;
    }

    private static bool CanUseUtf8TextFallback(ReadOnlyMemory<byte> body, ContentType? contentType)
    {
        if (HasBinaryMediaType(contentType))
        {
            return false;
        }

        if (HasBinaryControlBytes(body.Span))
        {
            return false;
        }

        return CanDecodeText(body, StrictUtf8);
    }

    private static Encoding CreateStrictEncoding(Encoding encoding)
    {
        return Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    private static bool HasBinaryControlBytes(ReadOnlySpan<byte> body)
    {
        foreach (var value in body)
        {
            if (value == 0)
            {
                return true;
            }

            if (value < 0x09)
            {
                return true;
            }

            if (value is > 0x0D and < 0x20)
            {
                return true;
            }
        }

        return false;
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

        if (mediaType.StartsWith("font/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/gzip", StringComparison.OrdinalIgnoreCase))
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

        if (mediaType.Equals("application/wasm", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/zip", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool HasDefaultUtf8TextMediaType(ContentType? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        var mediaType = contentType.MediaType;

        if (HasJsonMediaType(mediaType))
        {
            return true;
        }

        if (mediaType.Equals("application/graphql", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
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

    private static bool HasTextualMediaType(ContentType? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        var mediaType = contentType.MediaType;

        if (mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (HasDefaultUtf8TextMediaType(contentType))
        {
            return true;
        }

        if (HasXmlMediaType(mediaType))
        {
            return true;
        }

        if (mediaType.Equals("application/ecmascript", StringComparison.OrdinalIgnoreCase))
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

}
