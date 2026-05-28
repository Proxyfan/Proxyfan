namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static factory helpers for <see cref="HypertextTransferProtocolForwardingOutcome" />.
///     Held in a separate static class so each discriminator (Failure, Standard, Streamed)
///     is a one-line construction at the call site without violating ATXCS011 (which forbids
///     static methods on non-static classes).
/// </summary>
public static class HypertextTransferProtocolForwardingOutcomes
{
    /// <summary>
    ///     Returns a failure outcome. The caller must fail the flow and stop the keep-alive loop.
    /// </summary>
    /// <returns>A failure outcome.</returns>
    public static HypertextTransferProtocolForwardingOutcome Failure()
    {
        var outcome = new HypertextTransferProtocolForwardingOutcome
        {
            IsFailure = true,
        };
        return outcome;
    }

    /// <summary>
    ///     Returns an outcome that carries a fully-read response exchange for the response-phase
    ///     pipeline to process.
    /// </summary>
    /// <param name="exchange">The response exchange ready for the response-phase pipeline.</param>
    /// <returns>A standard outcome.</returns>
    public static HypertextTransferProtocolForwardingOutcome Standard(HypertextTransferProtocolProxyResponseExchange exchange)
    {
        var outcome = new HypertextTransferProtocolForwardingOutcome
        {
            Exchange = exchange,
        };
        return outcome;
    }

    /// <summary>
    ///     Returns an outcome indicating the forwarding step already streamed the response to
    ///     the client (e.g. SSE). The caller must NOT run the response-phase pipeline.
    /// </summary>
    /// <returns>A streaming outcome.</returns>
    public static HypertextTransferProtocolForwardingOutcome Streamed()
    {
        var outcome = new HypertextTransferProtocolForwardingOutcome
        {
            IsStreaming = true,
        };
        return outcome;
    }
}
