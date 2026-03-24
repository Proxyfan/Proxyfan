using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>Base record for all proxy-specific domain errors.</summary>
/// <param name="Code">Machine-readable error code.</param>
/// <param name="Message">Human-readable error description.</param>
/// <param name="InnerException">Optional underlying exception.</param>
public abstract record ProxyError(string Code, string Message, Exception? InnerException = null)
    : DomainError(Code, Message, InnerException);

/// <summary>Error raised when the proxy listener fails to bind to the configured port.</summary>
/// <param name="Port">The port number that could not be bound.</param>
/// <param name="InnerException">The underlying bind exception.</param>
public sealed record ProxyBindError(int Port, Exception InnerException)
    : ProxyError(
        "PROXY_BIND_FAILED",
        $"Failed to bind proxy listener to port {Port}: {InnerException.Message}",
        InnerException);

/// <summary>Error raised when <c>StartAsync</c> is called while the proxy is already running or starting.</summary>
public sealed record ProxyAlreadyRunningError()
    : ProxyError("PROXY_ALREADY_RUNNING", "The proxy server is already running.");

/// <summary>Error raised when <c>StopAsync</c> is called while the proxy is already stopped or stopping.</summary>
public sealed record ProxyNotRunningError()
    : ProxyError("PROXY_NOT_RUNNING", "The proxy server is not running.");

/// <summary>Error raised when a lifecycle operation fails due to an unexpected exception.</summary>
/// <param name="Operation">The operation that failed (e.g., <c>"Start"</c>, <c>"Stop"</c>).</param>
/// <param name="InnerException">The exception that caused the failure.</param>
public sealed record ProxyFaultedError(string Operation, Exception InnerException)
    : ProxyError(
        "PROXY_FAULTED",
        $"The proxy server encountered an unexpected error during {Operation}: {InnerException.Message}",
        InnerException);
