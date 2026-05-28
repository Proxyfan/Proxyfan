using System;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parser for HTTP Basic Authentication header values per RFC 7617. Accepts both the
///     raw header value (with the "Basic " scheme prefix) and the bare base64 payload.
/// </summary>
public static class BasicAuthenticationParser
{
    private const string SchemePrefix = "Basic ";

    /// <summary>
    ///     Parses the supplied Authorization header value into username/password. Returns
    ///     null when the value is null/blank, does not start with "Basic ", or fails base64
    ///     decoding, or the decoded payload has no colon.
    /// </summary>
    /// <param name="headerValue">The raw Authorization header value.</param>
    /// <returns>The parsed credentials, or null.</returns>
    public static BasicAuthenticationCredentials? Parse(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        var trimmed = headerValue.Trim();
        string base64;

        if (trimmed.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            base64 = trimmed[SchemePrefix.Length..].Trim();
        }
        else
        {
            base64 = trimmed;
        }

        byte[] decodedBytes;

        try
        {
            decodedBytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }

        var decoded = Encoding.UTF8.GetString(decodedBytes);
        var separatorIndex = decoded.IndexOf(':', StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            return null;
        }

        var userName = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];
        var credentials = new BasicAuthenticationCredentials(userName, password);
        return credentials;
    }
}
