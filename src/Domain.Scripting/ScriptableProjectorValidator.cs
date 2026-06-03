using System;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Pure grammar helpers used by <see cref="ScriptableProjector" /> to validate the
///     script-editable surface (URL, method, status code, reason phrase, header name and
///     value) against the HTTP/1.1 wire format (RFC 7230 / RFC 9110) before materializing
///     an immutable domain request/response. Keeping the helpers separate avoids polluting
///     the projector with grammar logic and makes them independently testable.
/// </summary>
public static class ScriptableProjectorValidator
{
    /// <summary>
    ///     Set of tchar punctuation characters from RFC 7230 §3.2.6, in addition to ALPHA and
    ///     DIGIT which are checked by range.
    /// </summary>
    private const string TokenPunctuation = "!#$%&'*+-.^_`|~";

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="name" /> is a non-empty HTTP
    ///     token per RFC 7230 §3.2.6 (the same grammar used for method names and field-names).
    /// </summary>
    /// <param name="name">The header name to validate.</param>
    /// <returns><see langword="true" /> when the name is a valid token; otherwise <see langword="false" />.</returns>
    public static bool HasValidHeaderName(string? name)
    {
        return HasValidToken(name);
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="value" /> contains only header
    ///     field-value bytes (RFC 7230 §3.2.6 / RFC 9110 §5.5): HTAB, SP, VCHAR (0x21–0x7E),
    ///     and obs-text (0x80–0xFF). All other C0 controls (0x00–0x1F except HTAB) and DEL
    ///     (0x7F) are rejected because they corrupt or terminate the header line on the wire.
    ///     Empty values are allowed.
    /// </summary>
    /// <param name="value">The header value to validate.</param>
    /// <returns><see langword="true" /> when the value is a valid field-value; otherwise <see langword="false" />.</returns>
    public static bool HasValidHeaderValue(string? value)
    {
        if (value is null)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!HasFieldValueCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="method" /> is a non-empty HTTP
    ///     token per RFC 7230 §3.1.1 (the same grammar as <see cref="HasValidHeaderName" />).
    /// </summary>
    /// <param name="method">The HTTP method to validate.</param>
    /// <returns><see langword="true" /> when the method is a valid token; otherwise <see langword="false" />.</returns>
    public static bool HasValidMethod(string? method)
    {
        return HasValidToken(method);
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="reasonPhrase" /> is allowed by
    ///     RFC 7230 §3.1.2: zero or more HTAB / SP / VCHAR / obs-text bytes. CR, LF, and NUL
    ///     are rejected because they would terminate or corrupt the status line.
    /// </summary>
    /// <param name="reasonPhrase">The reason phrase to validate.</param>
    /// <returns><see langword="true" /> when the phrase is well-formed; otherwise <see langword="false" />.</returns>
    public static bool HasValidReasonPhrase(string? reasonPhrase)
    {
        if (reasonPhrase is null)
        {
            return false;
        }

        foreach (var character in reasonPhrase)
        {
            if (!HasReasonPhraseCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="statusCode" /> is a three-digit
    ///     status code (RFC 7230 §3.1.2 status-code = 3DIGIT). Values outside 100–999 cannot be
    ///     represented in the status line and are therefore rejected.
    /// </summary>
    /// <param name="statusCode">The status code to validate.</param>
    /// <returns><see langword="true" /> when the code is a valid 3-digit number; otherwise <see langword="false" />.</returns>
    public static bool HasValidStatusCode(int statusCode)
    {
        return statusCode is >= 100 and <= 999;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="url" /> parses as an absolute
    ///     HTTP(S) URI with a non-empty host that is usable as an HTTP request target.
    ///     Relative URLs, unparseable strings, non-HTTP schemes (e.g. file://, ftp://), and
    ///     authority-less inputs such as <c>http:/path</c> are rejected because they would
    ///     otherwise produce ambiguous data when handed to the proxy pipeline.
    /// </summary>
    /// <param name="url">The request URL to validate.</param>
    /// <returns><see langword="true" /> when the URL is a valid absolute HTTP(S) URI; otherwise <see langword="false" />.</returns>
    public static bool HasValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return !string.IsNullOrEmpty(parsed.Host);
    }

    private static bool HasFieldValueCharacter(char character)
    {
        if (character == '\t')
        {
            return true;
        }

        return character is (>= (char)0x20 and <= (char)0x7E) or (>= (char)0x80 and <= (char)0xFF);
    }

    private static bool HasReasonPhraseCharacter(char character)
    {
        if (character is '\t' or ' ')
        {
            return true;
        }

        if (character is >= (char)0x21 and <= (char)0x7E)
        {
            return true;
        }

        return character is >= (char)0x80 and <= (char)0xFF;
    }

    private static bool HasTokenCharacter(char character)
    {
        if (character is >= 'a' and <= 'z')
        {
            return true;
        }

        if (character is >= 'A' and <= 'Z')
        {
            return true;
        }

        if (character is >= '0' and <= '9')
        {
            return true;
        }

        return TokenPunctuation.Contains(character);
    }

    private static bool HasValidToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        foreach (var character in token)
        {
            if (!HasTokenCharacter(character))
            {
                return false;
            }
        }

        return true;
    }
}
