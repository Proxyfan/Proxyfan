using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Throttling;
using Proxyfan.Domain.Traffic;
using System;

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
    ///     Gets the optional certificate authority provider used to serve the local
    ///     provisioning landing page when a client navigates to the magic provisioning
    ///     host. When <see langword="null" />, provisioning requests are forwarded as
    ///     ordinary HTTP traffic and will fail to resolve.
    /// </summary>
    public MutableCertificateAuthorityProvider? CertificateAuthorityProvider { get; init; }

    /// <summary>
    ///     Gets the domain event bus used to publish traffic capture events.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the optional DNS override resolver used to redirect outbound connections to a
    ///     user-configured IP address when a matching override exists in the
    ///     <see cref="DomainNameSystemOverrideMap" />. When <see langword="null" />, the proxy
    ///     uses operating-system DNS resolution.
    /// </summary>
    public UpstreamHostResolver? HostResolver { get; init; }

    /// <summary>
    ///     Gets the logger used for structured diagnostic output.
    /// </summary>
    public required ILogger<HypertextTransferProtocolProxyHandler> Logger { get; init; }

    /// <summary>
    ///     Gets the rule engine used to evaluate request- and response-phase rules.
    /// </summary>
    public required IRuleEngine RuleEngine { get; init; }

    /// <summary>
    ///     Gets the optional scripting handler that runs user-defined C# scripts before the
    ///     request leaves the proxy and after the response is received. When
    ///     <see langword="null" /> or when no script is active, traffic passes through unchanged.
    /// </summary>
    public IScriptingHandler? ScriptingHandler { get; init; }

    /// <summary>
    ///     Gets the optional Server-Sent Events store used to capture <c>text/event-stream</c>
    ///     responses while the relay forwards them verbatim to the client. When
    ///     <see langword="null" />, SSE responses are still streamed correctly but the captured
    ///     events are not retained for inspection.
    /// </summary>
    public IServerSentEventsStore? ServerSentEventsStore { get; init; }

    /// <summary>
    ///     Gets the optional throttle profile holder used to bandwidth-limit response writes.
    ///     When <see langword="null" /> or when no profile is active, writes pass through unthrottled.
    /// </summary>
    public MutableThrottleProfile? ThrottleProfile { get; init; }

    /// <summary>
    ///     Gets the time provider used for WebSocket message timestamps. Defaults to
    ///     <see cref="TimeProvider.System" /> when not supplied.
    /// </summary>
    public TimeProvider? TimeProvider { get; init; }

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

    /// <summary>
    ///     Gets the optional WebSocket store used to capture frames when an upgrade succeeds.
    ///     When <see langword="null" />, WebSocket upgrades still tunnel correctly but messages
    ///     are not retained for inspection.
    /// </summary>
    public IWebSocketStore? WebSocketStore { get; init; }
}
