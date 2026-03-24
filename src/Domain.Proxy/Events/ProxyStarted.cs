using System;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when the proxy server successfully starts listening on a port.
/// </summary>
/// <param name="Port">The port the proxy is now listening on.</param>
/// <param name="Timestamp">The UTC instant at which the proxy started.</param>
public sealed record ProxyStarted(int Port, DateTimeOffset Timestamp) : IDomainEvent;
