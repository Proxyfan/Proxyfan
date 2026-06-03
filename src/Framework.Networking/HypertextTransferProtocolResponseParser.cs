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
    ///     A header that is present but malformed, negative, or in conflict with itself across
    ///     comma-joined tokens or repeated header lines is treated as invalid and returns
    ///     <c>-1</c>; callers that need to distinguish &quot;absent&quot; from &quot;invalid&quot;
    ///     must inspect the response headers directly or go through
    ///     <see cref="HypertextTransferProtocolBodyFramingClassifier.ClassifyResponse" />,
    ///     which surfaces <see cref="HypertextTransferProtocolBodyFraming.Invalid" /> for
    ///     malformed framing.
    /// </summary>
    /// <param name="response">
    ///     The parsed response data.
    /// </param>
    /// <returns>
    ///     The parsed content length, or <c>-1</c> when the header is absent or invalid.
    /// </returns>
    public static long GetContentLength(HypertextTransferProtocolResponseData response)
    {
        var values = response.Headers.GetAll("Content-Length");

        if (values.Length == 0)
        {
            return -1;
        }

        if (ContentLengthParser.HasValidContentLength(values, out var parsed))
        {
            return parsed;
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

        if (headerSectionLength <= 0)
        {
            return string.Empty;
        }

        return headerText.Substring(headerSectionStartIndex, headerSectionLength);
    }

    private static HypertextTransferProtocolResponseStatusLine? ParseStatusLine(string statusLine)
    {
        var firstSpaceIndex = statusLine.IndexOf(' ');
        if (firstSpaceIndex <= 0)
        {
            return null;
        }

        var secondSpaceIndex = statusLine.IndexOf(' ', firstSpaceIndex + 1);
        if (secondSpaceIndex < 0)
        {
            return null;
        }

        var version = statusLine[..firstSpaceIndex];
        var statusCodeText = statusLine.Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex - 1);
        var reasonPhrase = statusLine[(secondSpaceIndex + 1)..];

        if (!int.TryParse(statusCodeText, out var statusCode))
        {
            return null;
        }

        if (!version.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parsedStatusLine = new HypertextTransferProtocolResponseStatusLine(statusCode, reasonPhrase, version);
        return parsedStatusLine;
    }
}