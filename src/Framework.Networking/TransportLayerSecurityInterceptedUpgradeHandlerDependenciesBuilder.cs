namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helper that builds a
///     <see cref="TransportLayerSecurityInterceptedUpgradeHandlerDependencies" /> from the
///     surrounding <see cref="TransportLayerSecurityInterceptorHandler" />. Lives outside
///     the handler class to keep that class within the analyzer-enforced line budget
///     (ATXCS034).
/// </summary>
public static class TransportLayerSecurityInterceptedUpgradeHandlerDependenciesBuilder
{
    /// <summary>
    ///     Builds a dependency bundle for the upgrade handler from the broader interceptor
    ///     dependencies. Forwards the rule engine so intercepted Upgrade responses go through
    ///     the same response-phase pipeline as normal intercepted HTTPS responses.
    /// </summary>
    /// <param name="dependencies">The broader interceptor dependencies.</param>
    /// <returns>The upgrade-handler dependency bundle.</returns>
    public static TransportLayerSecurityInterceptedUpgradeHandlerDependencies Build(
        TransportLayerSecurityInterceptorHandlerDependencies dependencies)
    {
        return new TransportLayerSecurityInterceptedUpgradeHandlerDependencies
        {
            EventBus = dependencies.EventBus,
            Logger = dependencies.Logger,
            RuleEngine = dependencies.RuleEngine,
            TimeProvider = dependencies.TimeProvider ?? System.TimeProvider.System,
            TrafficStore = dependencies.TrafficStore,
            WebSocketStore = dependencies.WebSocketStore,
        };
    }
}
