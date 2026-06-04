using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Dependency bundle for <see cref="TransportLayerSecurityInterceptedUpgradeHandler" />.
///     Bundling lets the handler accept the rule engine and event bus that drive
///     response-phase policy for intercepted HTTP/1.1 Upgrade exchanges
///     without exceeding the analyzer-enforced constructor parameter limit (ATXCS022).
/// </summary>
public sealed class TransportLayerSecurityInterceptedUpgradeHandlerDependencies
{
    /// <summary>
    ///     Gets the domain event bus used to publish captured flow events.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the logger used for diagnostics.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     Gets the optional rule engine used to evaluate response-phase rules against the
    ///     upstream upgrade response (Modify Response, No Caching, ...).
    /// </summary>
    public IRuleEngine? RuleEngine { get; init; }

    /// <summary>
    ///     Gets the time provider used for WebSocket message timestamps.
    /// </summary>
    public required TimeProvider TimeProvider { get; init; }

    /// <summary>
    ///     Gets the traffic store that retains completed flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }

    /// <summary>
    ///     Gets the optional WebSocket store that retains captured WebSocket messages.
    /// </summary>
    public IWebSocketStore? WebSocketStore { get; init; }
}
