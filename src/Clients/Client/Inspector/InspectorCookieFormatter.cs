using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Helpers that render request and response cookie text blocks from message data
///     for the Inspector Cookies tabs.
/// </summary>
public static class InspectorCookieFormatter
{
    /// <summary>
    ///     Formats the request <c>Cookie</c> header into a fixed-width text table.
    /// </summary>
    /// <param name="request">The request whose Cookie header is rendered.</param>
    /// <returns>The formatted cookies, or an empty string when no Cookie header is present.</returns>
    public static string FormatRequest(HypertextTransferProtocolRequestData request)
    {
        var header = request.Headers.Get("Cookie");

        if (header is null)
        {
            return string.Empty;
        }

        var cookies = HypertextTransferProtocolCookieParser.ParseRequestCookies(header);
        return HypertextTransferProtocolCookieTextFormatter.Format(cookies);
    }

    /// <summary>
    ///     Formats the response <c>Set-Cookie</c> headers into a fixed-width text table.
    /// </summary>
    /// <param name="response">The response whose Set-Cookie headers are rendered.</param>
    /// <returns>The formatted cookies, or an empty string when no Set-Cookie header is present.</returns>
    public static string FormatResponse(HypertextTransferProtocolResponseData response)
    {
        var values = response.Headers.GetAll("Set-Cookie");

        if (values.Length == 0)
        {
            return string.Empty;
        }

        var cookies = new List<HypertextTransferProtocolCookie>();

        foreach (var value in values)
        {
            var parsed = HypertextTransferProtocolCookieParser.ParseSetCookie(value);

            if (parsed is not null)
            {
                cookies.Add(parsed);
            }
        }

        return HypertextTransferProtocolCookieTextFormatter.Format(cookies);
    }
}
