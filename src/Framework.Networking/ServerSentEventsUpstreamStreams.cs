using System.IO;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Provides helpers for resolving the upstream read stream that an SSE relay should drain
///     from. When the response-header read prefetched body bytes ahead of the framing boundary,
///     those bytes are replayed from a <see cref="PrefixedReadStream" /> wrapper so the relay
///     sees a contiguous byte sequence.
/// </summary>
public static class ServerSentEventsUpstreamStreams
{
    /// <summary>
    ///     Resolves the upstream read stream for the relay. If the request carries prefetched
    ///     bytes from the header read, they are prepended via <see cref="PrefixedReadStream" />.
    /// </summary>
    /// <param name="request">The streaming request bundle.</param>
    /// <returns>The stream that the SSE relay must drain.</returns>
    public static Stream Resolve(ServerSentEventsStreamRequest request)
    {
        if (request.UpstreamPrefetched.Length == 0)
        {
            return request.UpstreamStream;
        }

        var prefixedStream = new PrefixedReadStream(request.UpstreamPrefetched, request.UpstreamStream);
        return prefixedStream;
    }
}
