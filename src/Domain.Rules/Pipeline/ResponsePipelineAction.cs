using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Rules.Pipeline;

/// <summary>
///     Defines the immediate action that a response-phase rule directs the proxy pipeline to take.
/// </summary>
public abstract record ResponsePipelineAction
{
    /// <summary>
    ///     Indicates that the rule modified the response headers or body before delivery to the client.
    /// </summary>
    public sealed record ModifyResponse : ResponsePipelineAction
    {
        /// <summary>
        ///     Gets the response after modification.
        /// </summary>
        public HypertextTransferProtocolResponseData ModifiedResponse { get; }

        /// <summary>
        ///     Initializes a new <see cref="ModifyResponse" /> action with the modified response.
        /// </summary>
        /// <param name="modifiedResponse">The response after modification.</param>
        public ModifyResponse(HypertextTransferProtocolResponseData modifiedResponse)
        {
            ModifiedResponse = modifiedResponse;
        }
    }
}
