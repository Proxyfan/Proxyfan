using System;
using System.Collections.Generic;
using System.Globalization;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pure static helpers used by <see cref="HypertextTransferProtocolVersion2Orchestrator" />.
///     Lives in its own static class to satisfy the static-in-non-static-class rule (ATXCS011).
/// </summary>
public static class HypertextTransferProtocolVersion2OrchestratorHelpers
{
    /// <summary>
    ///     Builds a <see cref="HypertextTransferProtocolVersion2FrameDescriptor" /> that
    ///     reproduces the supplied frame header verbatim when forwarded back through
    ///     <see cref="HypertextTransferProtocolVersion2FrameWriter" />.
    /// </summary>
    /// <param name="frame">The parsed frame.</param>
    /// <returns>The matching descriptor.</returns>
    public static HypertextTransferProtocolVersion2FrameDescriptor BuildDescriptor(HypertextTransferProtocolVersion2Frame frame)
    {
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            Flags = frame.Header.Flags,
            PayloadLength = frame.Header.Length,
            StreamIdentifier = frame.Header.StreamIdentifier,
            Type = frame.Header.Type,
        };
        return descriptor;
    }

    /// <summary>
    ///     Builds an HTTP/1.1-shaped
    ///     <see cref="Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData" /> from a
    ///     decoded HTTP/2 response header list and accumulated body bytes.
    /// </summary>
    /// <param name="headers">The decoded response header fields (must include a <c>:status</c> pseudo-header).</param>
    /// <param name="body">The accumulated response body bytes from DATA frames.</param>
    /// <returns>The translated response, or <see langword="null" /> when the <c>:status</c> pseudo-header is missing or invalid.</returns>
    public static Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData? BuildResponseFromHeaders(
        IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> headers,
        ReadOnlyMemory<byte> body)
    {
        var statusCode = 0;
        var collection = Proxyfan.Domain.Traffic.HeaderCollection.Empty;
        for (var index = 0; index < headers.Count; index++)
        {
            var field = headers[index];
            if (string.Equals(field.Name, ":status", StringComparison.Ordinal))
            {
                if (!int.TryParse(field.Value, CultureInfo.InvariantCulture, out var parsed))
                {
                    return null;
                }

                statusCode = parsed;
                continue;
            }

            if (field.Name.StartsWith(':'))
            {
                continue;
            }

            collection = collection.Add(field.Name, field.Value);
        }

        if (statusCode is < 100 or > 999)
        {
            return null;
        }

        var parameters = new Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = collection,
            ReasonPhrase = string.Empty,
            StatusCode = statusCode,
            Version = "HTTP/2",
        };
        var response = new Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData(parameters);
        return response;
    }

    /// <summary>
    ///     Factory used by the concurrent dictionary in the orchestrator to lazily create a
    ///     per-stream <see cref="HypertextTransferProtocolVersion2CaptureState" />.
    /// </summary>
    /// <param name="streamIdentifier">The 31-bit HTTP/2 stream identifier.</param>
    /// <returns>A fresh capture state tracker.</returns>
    public static HypertextTransferProtocolVersion2CaptureState CreateCaptureState(uint streamIdentifier)
    {
        var capture = new HypertextTransferProtocolVersion2CaptureState(streamIdentifier);
        return capture;
    }

    /// <summary>
    ///     Returns a copy of <paramref name="response" /> with
    ///     <see cref="Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData.Body" />
    ///     replaced by <paramref name="body" />.
    /// </summary>
    /// <param name="response">The response whose body should be replaced.</param>
    /// <param name="body">The new body bytes.</param>
    /// <returns>A copy with the updated body.</returns>
    public static Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData ReplaceResponseBody(
        Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData response,
        ReadOnlyMemory<byte> body)
    {
        var parameters = new Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = response.Headers,
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            Version = response.Version,
        };
        var copy = new Proxyfan.Domain.Traffic.HypertextTransferProtocolResponseData(parameters);
        return copy;
    }
}
