using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Rules.Pipeline;

/// <summary>
///     Defines the immediate action that a request-phase rule directs the proxy pipeline to take.
/// </summary>
public abstract record RequestPipelineAction
{
    /// <summary>
    ///     Indicates that the request must be rejected outright; the proxy returns a 403 to the client.
    /// </summary>
    public sealed record Block : RequestPipelineAction;

    /// <summary>
    ///     Indicates that the rule modified the request headers but the pipeline must still
    ///     forward the request unchanged in destination.
    /// </summary>
    public sealed record ModifyRequest : RequestPipelineAction
    {
        /// <summary>
        ///     Gets the request with modified headers.
        /// </summary>
        public HypertextTransferProtocolRequestData ModifiedRequest { get; }

        /// <summary>
        ///     Initializes a new <see cref="ModifyRequest" /> action with the modified request.
        /// </summary>
        /// <param name="modifiedRequest">The request with modified headers.</param>
        public ModifyRequest(HypertextTransferProtocolRequestData modifiedRequest)
        {
            ModifiedRequest = modifiedRequest;
        }
    }

    /// <summary>
    ///     Indicates that a breakpoint rule aborted the request; the proxy closes the connection
    ///     without sending a response. Semantically distinct from <see cref="Block" /> (which
    ///     returns a 403 to the client) because a breakpoint abort is an operator-driven decision
    ///     rather than a policy-driven rejection.
    /// </summary>
    public sealed record Pause : RequestPipelineAction;

    /// <summary>
    ///     Indicates that the request URL has been rewritten and the pipeline should forward
    ///     the modified request.
    /// </summary>
    public sealed record Redirect : RequestPipelineAction
    {
        /// <summary>
        ///     Gets the request with the rewritten URL and headers.
        /// </summary>
        public HypertextTransferProtocolRequestData RewrittenRequest { get; }

        /// <summary>
        ///     Initializes a new <see cref="Redirect" /> action with the rewritten request.
        /// </summary>
        /// <param name="rewrittenRequest">The request with the rewritten URL and headers.</param>
        public Redirect(HypertextTransferProtocolRequestData rewrittenRequest)
        {
            RewrittenRequest = rewrittenRequest;
        }
    }

    /// <summary>
    ///     Indicates that the proxy must serve a locally-configured response without contacting
    ///     the upstream server (response-phase rules still apply).
    /// </summary>
    public sealed record ServeLocalResponse : RequestPipelineAction
    {
        /// <summary>
        ///     Gets the local response to serve.
        /// </summary>
        public HypertextTransferProtocolResponseData LocalResponse { get; }

        /// <summary>
        ///     Initializes a new <see cref="ServeLocalResponse" /> action with the local response.
        /// </summary>
        /// <param name="localResponse">The local response to serve.</param>
        public ServeLocalResponse(HypertextTransferProtocolResponseData localResponse)
        {
            LocalResponse = localResponse;
        }
    }
}
