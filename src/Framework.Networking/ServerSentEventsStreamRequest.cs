using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using System.IO;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object bundling everything required to execute a Server-Sent Events streaming
///     relay. Required to keep
///     <see cref="ServerSentEventsStreamHandler.HandleAsync" /> within the analyzer's
///     four-parameter limit (ATXCS022).
/// </summary>
public sealed class ServerSentEventsStreamRequest
{
    /// <summary>
    ///     Gets the client connection that receives the relayed response.
    /// </summary>
    public required IProxyConnection Connection { get; init; }

    /// <summary>
    ///     Gets the request after rules/scripting/breakpoint modifications. Used to determine
    ///     whether the client connection can stay alive after the SSE stream terminates.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the traffic flow that accumulates capture data for this exchange.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the verbatim response header bytes (including trailing CRLF CRLF) that will be
    ///     written to the client before streaming begins.
    /// </summary>
    public required byte[] ResponseHeaderBytes { get; init; }

    /// <summary>
    ///     Gets the response headers parsed from the upstream stream. The body has not yet been
    ///     read; the handler streams it through <see cref="ServerSentEventsRelay" />.
    /// </summary>
    public required HypertextTransferProtocolResponseData ResponseHeaders { get; init; }

    /// <summary>
    ///     Gets the bytes the upstream pipe reader consumed past the response headers before
    ///     the SSE relay took over. These bytes are prepended to the relay read source so no
    ///     event data is lost.
    /// </summary>
    public required byte[] UpstreamPrefetched { get; init; }

    /// <summary>
    ///     Gets the upstream read/write stream owning the live TCP connection. The handler
    ///     reads body bytes from this stream and relays them to the client.
    /// </summary>
    public required Stream UpstreamStream { get; init; }
}
