namespace Proxyfan.Framework.Networking;

/// <summary>
///     Discriminated outcome returned by the HTTP/1.1 proxy handler's upstream forwarding
///     step. The forwarding can either:
///     <list type="bullet">
///         <item>Return a fully-read response exchange ready to flow through the response-phase pipeline.</item>
///         <item>Stream a long-lived response body (e.g. Server-Sent Events) directly to the client and complete the flow itself.</item>
///         <item>Fail because the upstream connection could not be established or the response could not be parsed.</item>
///     </list>
/// </summary>
public sealed class HypertextTransferProtocolForwardingOutcome
{
    /// <summary>
    ///     Gets the response exchange when the standard read path was taken. <see langword="null" />
    ///     when the outcome is streaming or failure.
    /// </summary>
    public HypertextTransferProtocolProxyResponseExchange? Exchange { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the upstream forwarding failed and the flow must be
    ///     aborted by the caller.
    /// </summary>
    public bool IsFailure { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the forwarding step took over the response writing
    ///     (e.g. streamed an SSE response). The caller must NOT run the response-phase pipeline
    ///     and must close the client connection because the underlying transport may no longer
    ///     be at an HTTP message boundary.
    /// </summary>
    public bool IsStreaming { get; init; }
}
