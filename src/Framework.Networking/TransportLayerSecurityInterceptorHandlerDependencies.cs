using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Traffic;

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
    ///     Gets the logger used for structured diagnostic output.
    /// </summary>
    public required ILogger<TransportLayerSecurityInterceptorHandler> Logger { get; init; }

    /// <summary>
    ///     Gets the optional rule engine used to evaluate request- and response-phase rules
    ///     for intercepted (decrypted) traffic.
    /// </summary>
    public IRuleEngine? RuleEngine { get; init; }

    /// <summary>
    ///     Gets the store that persists captured traffic flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }
}
