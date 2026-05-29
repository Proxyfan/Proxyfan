using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles the dependencies required by
///     <see cref="ReverseProxyHypertextTransferProtocolHandler" />. Required because the
///     analyzer constructor parameter count rule (ATXCS022) forbids more than four parameters
///     and the reverse-proxy HTTP handler needs more than that.
/// </summary>
public sealed class ReverseProxyHypertextTransferProtocolHandlerDependencies
{
    /// <summary>
    ///     Gets the domain event bus used to publish flow lifecycle events for captured
    ///     reverse-proxy traffic. The same bus the forward proxy uses, so reverse-proxy flows
    ///     surface in the traffic inspector alongside forward-proxy flows.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the logger used by the handler for diagnostics.
    /// </summary>
    public required ILogger<ReverseProxyHypertextTransferProtocolHandler> Logger { get; init; }

    /// <summary>
    ///     Gets the rule engine used to evaluate request and response rules for reverse-proxy
    ///     traffic — Block, Map Local, Modify, etc.
    /// </summary>
    public required IRuleEngine RuleEngine { get; init; }

    /// <summary>
    ///     Gets the time provider used for flow timestamps.
    /// </summary>
    public required TimeProvider TimeProvider { get; init; }

    /// <summary>
    ///     Gets the traffic store that retains completed reverse-proxy flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }
}
