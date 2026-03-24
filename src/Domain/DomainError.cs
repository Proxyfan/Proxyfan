using System;

namespace Proxyfan.Domain;

/// <summary>
///     Base record for all domain errors, carrying a machine-readable code and a
///     human-readable message.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g., <c>"PROXY_BIND_FAILED"</c>).</param>
/// <param name="Message">Human-readable error description.</param>
/// <param name="InnerException">Optional underlying exception that caused this error.</param>
public abstract record DomainError(string Code, string Message, Exception? InnerException = null);
