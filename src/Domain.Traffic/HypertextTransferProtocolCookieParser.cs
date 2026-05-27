using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Parser for HTTP Cookie and Set-Cookie headers (RFC 6265).
/// </summary>
public static class HypertextTransferProtocolCookieParser
{
    /// <summary>
    ///     Parses a Cookie request header value (RFC 6265 Â§ 5.4) into a list of name/value pairs.
    /// </summary>
    /// <param name="headerValue">The raw Cookie header value.</param>
    /// <returns>The parsed cookies (name/value only; no attributes).</returns>
    public static IReadOnlyList<HypertextTransferProtocolCookie> ParseRequestCookies(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return [];
        }

        var cookies = new List<HypertextTransferProtocolCookie>();
        var pairs = headerValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = pair[..separatorIndex];
            var value = pair[(separatorIndex + 1)..];
            var parameters = new HypertextTransferProtocolCookieParameters
            {
                Name = name,
                Value = value,
            };
            var cookie = new HypertextTransferProtocolCookie(parameters);
            cookies.Add(cookie);
        }

        return cookies;
    }

    /// <summary>
    ///     Parses a Set-Cookie response header value (RFC 6265 Â§ 5.2) into a cookie with
    ///     attributes (Domain, Path, Expires, Secure, HttpOnly, SameSite).
    /// </summary>
    /// <param name="headerValue">The raw Set-Cookie header value.</param>
    /// <returns>The parsed cookie, or null when the value cannot be parsed.</returns>
    public static HypertextTransferProtocolCookie? ParseSetCookie(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        var attributes = headerValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (attributes.Length == 0)
        {
            return null;
        }

        var nameValuePair = attributes[0];
        var separatorIndex = nameValuePair.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return null;
        }

        var state = new HypertextTransferProtocolCookieAttributeState();
        for (var index = 1; index < attributes.Length; index++)
        {
            ApplyAttribute(state, attributes[index]);
        }

        var name = nameValuePair[..separatorIndex];
        var value = nameValuePair[(separatorIndex + 1)..];
        var parameters = new HypertextTransferProtocolCookieParameters
        {
            Name = name,
            Value = value,
            Domain = state.Domain,
            Path = state.Path,
            Expires = state.Expires,
            SameSite = state.SameSite,
            IsSecure = state.IsSecureFlag,
            IsHypertextTransferProtocolOnly = state.IsHypertextTransferProtocolOnlyFlag,
        };
        var cookie = new HypertextTransferProtocolCookie(parameters);
        return cookie;
    }

    private static void ApplyAttribute(HypertextTransferProtocolCookieAttributeState state, string attribute)
    {
        var equalsIndex = attribute.IndexOf('=', StringComparison.Ordinal);

        if (equalsIndex < 0)
        {
            if (string.Equals(attribute, "Secure", StringComparison.OrdinalIgnoreCase))
            {
                state.IsSecureFlag = true;
            }
            else if (string.Equals(attribute, "HttpOnly", StringComparison.OrdinalIgnoreCase))
            {
                state.IsHypertextTransferProtocolOnlyFlag = true;
            }

            return;
        }

        var attributeName = attribute[..equalsIndex];
        var attributeValue = attribute[(equalsIndex + 1)..];

        if (string.Equals(attributeName, "Domain", StringComparison.OrdinalIgnoreCase))
        {
            state.Domain = attributeValue;
        }
        else if (string.Equals(attributeName, "Path", StringComparison.OrdinalIgnoreCase))
        {
            state.Path = attributeValue;
        }
        else if (string.Equals(attributeName, "Expires", StringComparison.OrdinalIgnoreCase))
        {
            state.Expires = attributeValue;
        }
        else if (string.Equals(attributeName, "SameSite", StringComparison.OrdinalIgnoreCase))
        {
            state.SameSite = attributeValue;
        }
    }
}
