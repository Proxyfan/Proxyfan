using Proxyfan.Domain.Traffic;
using System;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses HTTP/1.1 response headers into immutable traffic-domain response data.
/// </summary>
public static class HypertextTransferProtocolResponseParser
{
    private const string HeaderLineTerminator = "\r\n";
    private const string HeaderSectionTerminator = "\r\n\r\n";

    /// <summary>
    ///     Returns the numeric response content length when the header is present and valid.
    /// </summary>
    /// <param name="response">
    ///     The parsed response data.
    /// </param>
    /// <returns>
    ///     The parsed content length, or <c>-1</c> when the header is absent or invalid.
    /// </returns>
    public static long GetContentLength(HypertextTransferProtocolResponseData response)
    {
        var contentLength = response.Headers.Get("Content-Length");

        if (long.TryParse(contentLength, out var parsedContentLength) && parsedContentLength >= 0)
        {
            return parsedContentLength;
        }

        return -1;
    }

    /// <summary>
    ///     Parses raw response header bytes into a <see cref="HypertextTransferProtocolResponseData" />.
    /// </summary>
    /// <param name="headerBytes">
    ///     The raw response header bytes including the terminating blank line.
    /// </param>
    /// <returns>
    ///     The parsed response data, or <see langword="null" /> when the response is malformed.
    /// </returns>
    public static HypertextTransferProtocolResponseData? ParseHeaders(byte[] headerBytes)
    {
        var headerText = Encoding.ASCII.GetString(headerBytes);
        var statusLineEndIndex = headerText.IndexOf(HeaderLineTerminator, StringComparison.Ordinal);

        if (statusLineEndIndex < 0 || !headerText.EndsWith(HeaderSectionTerminator, StringComparison.Ordinal))
        {
            return null;
        }

        var statusLine = headerText[..statusLineEndIndex];
        var parsedStatusLine = ParseStatusLine(statusLine);

        if (parsedStatusLine is null)
        {
            return null;
        }

        var headerSection = ExtractHeaderSection(headerText, statusLineEndIndex);
        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            StatusCode = parsedStatusLine.StatusCode,
            ReasonPhrase = parsedStatusLine.ReasonPhrase,
            Version = parsedStatusLine.Version,
            Headers = headers,
            Body = ReadOnlyMemory<byte>.Empty,
        };
        var responseData = new HypertextTransferProtocolResponseData(parameters);
        return responseData;
    }

    private static string ExtractHeaderSection(string headerText, int statusLineEndIndex)
    {
        var headerSectionStartIndex = statusLineEndIndex + HeaderLineTerminator.Length;
        var headerSectionLength = headerText.Length - headerSectionStartIndex - HeaderSectionTerminator.Length;
        return headerText.Substring(headerSectionStartIndex, headerSectionLength);
    }

    private static HypertextTransferProtocolResponseStatusLine? ParseStatusLine(string statusLine)
    {
        var parts = statusLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 || !int.TryParse(parts[1], out var statusCode) || string.IsNullOrWhiteSpace(parts[2]))
        {
            return null;
        }

        if (!parts[0].StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parsedStatusLine = new HypertextTransferProtocolResponseStatusLine(statusCode, parts[2], parts[0]);
        return parsedStatusLine;
    }
}