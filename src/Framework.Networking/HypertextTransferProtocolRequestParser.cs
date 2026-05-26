using Proxyfan.Domain.Traffic;
using System;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses HTTP/1.1 request headers into immutable traffic-domain request data.
/// </summary>
public static class HypertextTransferProtocolRequestParser
{
    private const string HeaderLineTerminator = "\r\n";
    private const string HeaderSectionTerminator = "\r\n\r\n";

    /// <summary>
    ///     Returns the numeric request content length when the header is present and valid.
    /// </summary>
    /// <param name="request">
    ///     The parsed request data.
    /// </param>
    /// <returns>
    ///     The parsed content length, or <c>0</c> when the header is absent or invalid.
    /// </returns>
    public static long GetContentLength(HypertextTransferProtocolRequestData request)
    {
        var contentLength = request.Headers.Get("Content-Length");

        if (long.TryParse(contentLength, out var parsedContentLength) && parsedContentLength >= 0)
        {
            return parsedContentLength;
        }

        return 0;
    }

    /// <summary>
    ///     Parses raw request header bytes into a <see cref="HypertextTransferProtocolRequestData" />.
    /// </summary>
    /// <param name="headerBytes">
    ///     The raw request header bytes including the terminating blank line.
    /// </param>
    /// <returns>
    ///     The parsed request data, or <see langword="null" /> when the request is malformed.
    /// </returns>
    public static HypertextTransferProtocolRequestData? ParseHeaders(byte[] headerBytes)
    {
        var headerText = Encoding.ASCII.GetString(headerBytes);
        var requestLineEndIndex = headerText.IndexOf(HeaderLineTerminator, StringComparison.Ordinal);

        if (requestLineEndIndex < 0 || !headerText.EndsWith(HeaderSectionTerminator, StringComparison.Ordinal))
        {
            return null;
        }

        var requestLine = headerText[..requestLineEndIndex];
        var parsedRequestLine = ParseRequestLine(requestLine);

        if (parsedRequestLine is null)
        {
            return null;
        }

        var headerSection = ExtractHeaderSection(headerText, requestLineEndIndex);
        var headers = HypertextTransferProtocolHeaderParser.Parse(headerSection);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Method = parsedRequestLine.Method,
            RequestUri = parsedRequestLine.RequestTargetUri,
            Version = parsedRequestLine.Version,
            Headers = headers,
            Body = ReadOnlyMemory<byte>.Empty,
        };
        var requestData = new HypertextTransferProtocolRequestData(parameters);
        return requestData;
    }

    private static string ExtractHeaderSection(string headerText, int requestLineEndIndex)
    {
        var headerSectionStartIndex = requestLineEndIndex + HeaderLineTerminator.Length;
        var headerSectionLength = headerText.Length - headerSectionStartIndex - HeaderSectionTerminator.Length;

        if (headerSectionLength <= 0)
        {
            return string.Empty;
        }

        return headerText.Substring(headerSectionStartIndex, headerSectionLength);
    }

    private static HypertextTransferProtocolRequestLine? ParseRequestLine(string requestLine)
    {
        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 || !Uri.TryCreate(parts[1], UriKind.RelativeOrAbsolute, out Uri? requestTargetUri))
        {
            return null;
        }

        if (!parts[2].StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parsedRequestLine = new HypertextTransferProtocolRequestLine(parts[0], requestTargetUri, parts[2]);
        return parsedRequestLine;
    }
}