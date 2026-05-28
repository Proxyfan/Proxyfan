using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Detects whether an HTTP response is a Server-Sent Events stream by inspecting its
///     <c>Content-Type</c> header. A response qualifies when the media type is
///     <c>text/event-stream</c> (with optional parameters per RFC 9110 §8.3).
/// </summary>
public static class ServerSentEventsResponseDetector
{
    private const string EventStreamMediaType = "text/event-stream";

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="response" /> declares a
    ///     <c>text/event-stream</c> body via its <c>Content-Type</c> header.
    /// </summary>
    /// <param name="response">The response headers to inspect.</param>
    /// <returns><see langword="true" /> when the response is an SSE stream.</returns>
    public static bool HasServerSentEventsResponse(HypertextTransferProtocolResponseData response)
    {
        var contentType = response.Headers.Get("Content-Type");

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var semicolonIndex = contentType.IndexOf(';');
        var mediaType = semicolonIndex < 0 ? contentType : contentType[..semicolonIndex];
        var trimmed = mediaType.Trim();
        return string.Equals(trimmed, EventStreamMediaType, StringComparison.OrdinalIgnoreCase);
    }
}
