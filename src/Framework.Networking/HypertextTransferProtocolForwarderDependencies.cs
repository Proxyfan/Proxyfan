using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles the dependencies required by <see cref="HypertextTransferProtocolForwarder" />.
///     Required because the analyzer constructor parameter count rule forbids more than four
///     parameters and the forwarder needs more than that.
/// </summary>
public sealed class HypertextTransferProtocolForwarderDependencies
{
    /// <summary>
    ///     Gets the domain event bus used by the SSE stream handler to publish response events.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the logger used by the SSE stream handler.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     Gets the optional SSE event store that retains captured events.
    /// </summary>
    public IServerSentEventsStore? ServerSentEventsStore { get; init; }

    /// <summary>
    ///     Gets the time source used by the SSE stream handler.
    /// </summary>
    public required TimeProvider TimeProvider { get; init; }

    /// <summary>
    ///     Gets the traffic store that retains completed flows after streaming finishes.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }

    /// <summary>
    ///     Gets the optional upstream proxy options monitor used to detect whether the request
    ///     should be chained through an upstream proxy.
    /// </summary>
    public IOptionsMonitor<UpstreamProxyOptions>? UpstreamProxy { get; init; }
}
