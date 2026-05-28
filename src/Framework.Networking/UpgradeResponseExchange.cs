namespace Proxyfan.Framework.Networking;

/// <summary>
///     Carries the parsed upstream response for an HTTP/1.1 upgrade exchange together with any
///     bytes that arrived in the same TCP read after the response headers. The proxy preserves
///     those prefetched bytes (typically the first WebSocket frame issued by the upstream server
///     immediately after the 101 Switching Protocols response) so they can be replayed into the
///     tunnel rather than dropped when the response-parsing pipe reader is completed.
/// </summary>
public sealed record UpgradeResponseExchange
{
    /// <summary>
    ///     Gets the bytes prefetched after the response headers.
    /// </summary>
    public byte[] PrefetchedBytes { get; }

    /// <summary>
    ///     Gets the parsed upstream upgrade response.
    /// </summary>
    public HypertextTransferProtocolProxyResponseExchange ResponseExchange { get; }

    /// <summary>
    ///     Initializes a new <see cref="UpgradeResponseExchange" />.
    /// </summary>
    /// <param name="responseExchange">The parsed upstream upgrade response.</param>
    /// <param name="prefetchedBytes">Bytes the response reader buffered after the headers that belong to the upgraded stream.</param>
    public UpgradeResponseExchange(
        HypertextTransferProtocolProxyResponseExchange responseExchange,
        byte[] prefetchedBytes)
    {
        ResponseExchange = responseExchange;
        PrefetchedBytes = prefetchedBytes;
    }
}
