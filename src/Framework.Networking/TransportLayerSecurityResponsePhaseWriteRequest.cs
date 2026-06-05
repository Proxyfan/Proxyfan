using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using System.IO.Pipelines;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object bundling the inputs required by
///     <see cref="TransportLayerSecurityResponsePhaseWriter.WriteAndPublishBookkeepingAsync" />.
///     Grouping keeps the helper under the analyzer-enforced parameter cap (ATXCS022).
/// </summary>
public sealed record TransportLayerSecurityResponsePhaseWriteRequest
{
    /// <summary>
    ///     Gets the domain event bus used to publish the completion event.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the response exchange to write to the client.
    /// </summary>
    public required HypertextTransferProtocolProxyResponseExchange Exchange { get; init; }

    /// <summary>
    ///     Gets the captured traffic flow that the bookkeeping refers to.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the traffic store that receives the captured flow.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }

    /// <summary>
    ///     Gets the client-facing pipe writer.
    /// </summary>
    public required PipeWriter Writer { get; init; }
}
