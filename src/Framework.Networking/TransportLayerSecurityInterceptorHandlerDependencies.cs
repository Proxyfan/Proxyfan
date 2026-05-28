using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Dependency bundle for <see cref="TransportLayerSecurityInterceptorHandler" />.
///     Bundling lets the handler accept the optional rule engine and breakpoint handler
///     without exceeding the analyzer-enforced constructor parameter limit (ATXCS022).
/// </summary>
public sealed class TransportLayerSecurityInterceptorHandlerDependencies
{
    /// <summary>
    ///     Gets the optional breakpoint handler used to pause intercepted requests/responses.
    /// </summary>
    public IBreakpointHandler? BreakpointHandler { get; init; }

    /// <summary>
    ///     Gets the TLS interception context used to resolve certificates and proxying rules.
    /// </summary>
    public required TransportLayerSecurityInterceptionContext Context { get; init; }

    /// <summary>
    ///     Gets the domain event bus used to publish captured traffic events.
    /// </summary>
    public required IDomainEventBus EventBus { get; init; }

    /// <summary>
    ///     Gets the optional DNS override resolver consulted before dialing the upstream
    ///     server during TLS interception so user-configured DNS spoofing entries are
    ///     honoured for intercepted HTTPS, tunneled HTTPS, and intercepted Upgrade flows.
    /// </summary>
    public UpstreamHostResolver? HostResolver { get; init; }

    /// <summary>
    ///     Gets the logger used for structured diagnostic output.
    /// </summary>
    public required ILogger<TransportLayerSecurityInterceptorHandler> Logger { get; init; }

    /// <summary>
    ///     Gets the optional rule engine used to evaluate request- and response-phase rules
    ///     for intercepted (decrypted) traffic.
    /// </summary>
    public IRuleEngine? RuleEngine { get; init; }

    /// <summary>
    ///     Gets the optional scripting handler used to invoke user scripts on intercepted
    ///     (decrypted) requests and responses.
    /// </summary>
    public IScriptingHandler? ScriptingHandler { get; init; }

    /// <summary>
    ///     Gets the optional Server-Sent Events store used to capture <c>text/event-stream</c>
    ///     responses observed over intercepted TLS streams. When <see langword="null" />, SSE
    ///     responses are still relayed correctly but events are not retained for inspection.
    /// </summary>
    public IServerSentEventsStore? ServerSentEventsStore { get; init; }

    /// <summary>
    ///     Gets the time provider used for WebSocket message timestamps. Defaults to
    ///     <see cref="System.TimeProvider.System" /> when not supplied.
    /// </summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>
    ///     Gets the store that persists captured traffic flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }

    /// <summary>
    ///     Gets the optional WebSocket store used to capture frames when an intercepted
    ///     wss:// upgrade succeeds. When <see langword="null" />, WebSocket upgrades still
    ///     tunnel correctly but messages are not retained for inspection.
    /// </summary>
    public IWebSocketStore? WebSocketStore { get; init; }
}
