using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Error raised when a lifecycle operation fails due to an unexpected exception.
/// </summary>
public sealed record ProxyFaultedError : ProxyError
{
    /// <summary>
    ///     Gets the operation that failed (e.g., <c>"Start"</c>, <c>"Stop"</c>).
    /// </summary>
    public string Operation { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyFaultedError" />.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., <c>"Start"</c>, <c>"Stop"</c>).</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public ProxyFaultedError(string operation, Exception innerException)
        : base(
            "PROXY_FAULTED",
            $"The proxy server encountered an unexpected error during {operation}: {innerException.Message}",
            innerException)
    {
        Operation = operation;
    }
}