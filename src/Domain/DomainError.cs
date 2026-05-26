using System;

namespace Proxyfan.Domain;

/// <summary>
///     Base record for all domain errors, carrying a machine-readable code and a
///     human-readable message.
/// </summary>
public abstract record DomainError
{
    /// <summary>
    ///     Gets the machine-readable error code (e.g., <c>"PROXY_BIND_FAILED"</c>).
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    ///     Gets the underlying exception that caused this error, or <see langword="null" /> if none.
    /// </summary>
    public Exception? InnerException { get; init; }

    /// <summary>
    ///     Gets the human-readable error description.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="DomainError" /> with the given code and message.
    /// </summary>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable error description.</param>
    protected DomainError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>
    ///     Initializes a new <see cref="DomainError" /> with the given code, message, and inner exception.
    /// </summary>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable error description.</param>
    /// <param name="innerException">The underlying exception that caused this error.</param>
    protected DomainError(string code, string message, Exception innerException)
    {
        Code = code;
        Message = message;
        InnerException = innerException;
    }
}