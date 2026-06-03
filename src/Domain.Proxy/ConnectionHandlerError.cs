using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Error raised when an <see cref="IConnectionHandler" /> throws an unhandled exception
///     while processing an accepted connection. The error carries only the exception type
///     name so that exception messages, stack traces, and other diagnostic details (which
///     may contain hostnames, request targets, local paths, or other sensitive data) are
///     not exposed across the domain event bus. The raw exception is kept on the local
///     logging path only, where redaction policy is enforced.
/// </summary>
public sealed record ConnectionHandlerError : ProxyError
{
    /// <summary>
    ///     Gets the fully qualified type name of the exception that caused the failure.
    ///     The exception message and stack trace are intentionally omitted to preserve
    ///     the proxy pipeline privacy boundary.
    /// </summary>
    public string ExceptionTypeName { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ConnectionHandlerError" /> from the given exception.
    ///     Only the exception's type name is captured; the message and stack trace are
    ///     not propagated to domain event subscribers.
    /// </summary>
    /// <param name="exception">The exception thrown by the connection handler.</param>
    public ConnectionHandlerError(Exception exception)
        : base(
            "CONNECTION_HANDLER_FAULTED",
            "A connection handler failed while processing an accepted connection.")
    {
        ExceptionTypeName = exception.GetType().FullName ?? exception.GetType().Name;
    }
}
