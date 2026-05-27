using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Throttling;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Dependency bundle for <see cref="HypertextTransferProtocolProxyHandler" />. Bundling
///     the dependencies lets the handler accept optional capabilities (upstream proxy
///     forwarding, throttling, breakpoints) without exceeding the analyzer-enforced
///     constructor parameter limit (ATXCS022).
/// </summary>
public sealed class HypertextTransferProtocolProxyHandlerDependencies
{
    /// <summary>
    ///     Gets the optional breakpoint handler used to pause traffic for user editing.
    /// </summary>
    public IBreakpointHandler? BreakpointHandler { get; init; }

    /// <summary>
    ///     Gets the domain event bus used to publish traffic capture events.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the logger used for structured diagnostic output.
    /// </summary>
    public required ILogger<HypertextTransferProtocolProxyHandler> Logger { get; init; }

    /// <summary>
    ///     Gets the rule engine used to evaluate request- and response-phase rules.
    /// </summary>
    public required IRuleEngine RuleEngine { get; init; }

    /// <summary>
    ///     Gets the optional throttle profile holder used to bandwidth-limit response writes.
    ///     When <see langword="null" /> or when no profile is active, writes pass through unthrottled.
    /// </summary>
    public MutableThrottleProfile? ThrottleProfile { get; init; }

    /// <summary>
    ///     Gets the store that persists captured traffic flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }

    /// <summary>
    ///     Gets the optional upstream proxy options monitor used to chain outbound requests
    ///     through a parent HTTP proxy. When <see langword="null" /> or disabled, requests are
    ///     sent directly to the origin server.
    /// </summary>
    public IOptionsMonitor<UpstreamProxyOptions>? UpstreamProxy { get; init; }
}
