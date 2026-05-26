namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Error raised when <c>StartAsync</c> is called while the proxy is already running or starting.
/// </summary>
public sealed record ProxyAlreadyRunningError : ProxyError
{
    /// <summary>
    ///     Initializes a new <see cref="ProxyAlreadyRunningError" />.
    /// </summary>
    public ProxyAlreadyRunningError() : base("PROXY_ALREADY_RUNNING", "The proxy server is already running.")
    {
    }
}