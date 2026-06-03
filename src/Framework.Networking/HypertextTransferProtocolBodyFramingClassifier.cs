using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Classifies the body framing strategy required to delimit an HTTP/1.x message body
///     per RFC 7230 § 3.3.3. Pure decision logic extracted from the pipe helpers so that the
///     decision can be unit-tested independently of any async pipe state.
/// </summary>
public static class HypertextTransferProtocolBodyFramingClassifier
{
    /// <summary>
    ///     Classifies a request body. A request has no body when neither
    ///     <c>Transfer-Encoding</c> nor <c>Content-Length</c> is present. Chunked wins when both
    ///     are present (RFC 7230 § 3.3.3 step 3).
    /// </summary>
    /// <param name="request">The parsed request data.</param>
    /// <returns>The body framing strategy to apply.</returns>
    public static HypertextTransferProtocolBodyFraming ClassifyRequest(HypertextTransferProtocolRequestData request)
    {
        if (HasChunkedTransferEncoding(request.Headers))
        {
            return HypertextTransferProtocolBodyFraming.Chunked;
        }

        var contentLength = request.Headers.Get("Content-Length");

        if (long.TryParse(contentLength, out var parsed) && parsed > 0)
        {
            return HypertextTransferProtocolBodyFraming.ContentLength;
        }

        return HypertextTransferProtocolBodyFraming.None;
    }

    /// <summary>
    ///     Classifies a response body. Status-codes that forbid a body (1xx, 204, 304) and the
    ///     HEAD method always produce <see cref="HypertextTransferProtocolBodyFraming.None" />
    ///     regardless of headers (RFC 7230 § 3.3.3 step 1). A present-but-invalid
    ///     <c>Content-Length</c> header maps to <see cref="HypertextTransferProtocolBodyFraming.Invalid" />
    ///     rather than <see cref="HypertextTransferProtocolBodyFraming.UntilClose" /> so the
    ///     caller can reject the response instead of silently waiting for connection close.
    /// </summary>
    /// <param name="response">The parsed response data.</param>
    /// <param name="requestMethod">The method of the request that produced this response.</param>
    /// <returns>The body framing strategy to apply.</returns>
    public static HypertextTransferProtocolBodyFraming ClassifyResponse(
        HypertextTransferProtocolResponseData response,
        string requestMethod)
    {
        if (HasHeadMethod(requestMethod) || HasNoBodyStatusCode(response.StatusCode))
        {
            return HypertextTransferProtocolBodyFraming.None;
        }

        if (HasChunkedTransferEncoding(response.Headers))
        {
            return HypertextTransferProtocolBodyFraming.Chunked;
        }

        var contentLengthValues = response.Headers.GetAll("Content-Length");

        if (contentLengthValues.Length == 0)
        {
            return HypertextTransferProtocolBodyFraming.UntilClose;
        }

        if (!ContentLengthParser.HasValidContentLength(contentLengthValues, out var parsed))
        {
            return HypertextTransferProtocolBodyFraming.Invalid;
        }

        return parsed == 0
            ? HypertextTransferProtocolBodyFraming.None
            : HypertextTransferProtocolBodyFraming.ContentLength;
    }

    private static bool HasChunkedTransferEncoding(HeaderCollection headers)
    {
        var transferEncoding = headers.Get("Transfer-Encoding");

        if (string.IsNullOrEmpty(transferEncoding))
        {
            return false;
        }

        var tokens = transferEncoding.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return false;
        }

        var lastToken = tokens[^1];
        return string.Equals(lastToken, "chunked", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasHeadMethod(string requestMethod)
    {
        return string.Equals(requestMethod, "HEAD", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasNoBodyStatusCode(int statusCode)
    {
        if (statusCode is >= 100 and < 200)
        {
            return true;
        }

        if (statusCode is 204 or 304)
        {
            return true;
        }

        return false;
    }
}
