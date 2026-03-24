using System;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when the proxy server stops listening, either via an explicit stop or dispose.
/// </summary>
/// <param name="Timestamp">The UTC instant at which the proxy stopped.</param>
public sealed record ProxyStopped(DateTimeOffset Timestamp) : IDomainEvent;
