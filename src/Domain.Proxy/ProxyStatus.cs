namespace Proxyfan.Domain.Proxy;

/// <summary>Represents the current lifecycle state of the proxy server.</summary>
public enum ProxyStatus
{
    /// <summary>The proxy is not running and no port is bound.</summary>
    Stopped,

    /// <summary>The proxy is in the process of starting (binding port, initializing).</summary>
    Starting,

    /// <summary>The proxy is actively listening for and accepting connections.</summary>
    Running,

    /// <summary>The proxy is in the process of shutting down gracefully.</summary>
    Stopping,

    /// <summary>
    ///     The proxy encountered an unrecoverable error and is not operational.
    ///     A subsequent call to <c>StartAsync</c> will attempt recovery.
    /// </summary>
    Faulted,
}
