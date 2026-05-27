namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Status of a reverse-proxy route in the engine: whether it is currently bound and
///     accepting connections, and whether the most recent health probe succeeded.
/// </summary>
public enum ReverseProxyRouteStatus
{
    /// <summary>
    ///     The route has not been started.
    /// </summary>
    Stopped = 0,

    /// <summary>
    ///     The route listener is bound and a recent health probe to the backend succeeded.
    /// </summary>
    Healthy = 1,

    /// <summary>
    ///     The route listener is bound but the most recent health probe failed.
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    ///     The route encountered a fatal error during start or while accepting.
    /// </summary>
    Faulted = 3,
}
