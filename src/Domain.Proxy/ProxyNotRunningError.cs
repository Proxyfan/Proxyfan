namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Error raised when <c>StopAsync</c> is called while the proxy is already stopped or stopping.
/// </summary>
public sealed record ProxyNotRunningError : ProxyError
{
    /// <summary>
    ///     Initializes a new <see cref="ProxyNotRunningError" />.
    /// </summary>
    public ProxyNotRunningError() : base("PROXY_NOT_RUNNING", "The proxy server is not running.")
    {
    }
}