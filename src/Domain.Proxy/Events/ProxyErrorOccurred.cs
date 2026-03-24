using System;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when the proxy server encounters an error during a lifecycle operation
///     (start, stop, or restart).
/// </summary>
/// <param name="Error">The domain error describing the failure.</param>
/// <param name="Timestamp">The UTC instant at which the error occurred.</param>
public sealed record ProxyErrorOccurred(ProxyError Error, DateTimeOffset Timestamp) : IDomainEvent;
