using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Base record for all proxy-specific domain errors.
/// </summary>
public abstract record ProxyError : DomainError
{
    /// <summary>
    ///     Initializes a new <see cref="ProxyError" /> with the given code and message.
    /// </summary>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable error description.</param>
    protected ProxyError(string code, string message) : base(code, message)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="ProxyError" /> with the given code, message, and inner exception.
    /// </summary>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable error description.</param>
    /// <param name="innerException">The underlying exception that caused this error.</param>
    protected ProxyError(string code, string message, Exception innerException) : base(code, message, innerException)
    {
    }
}