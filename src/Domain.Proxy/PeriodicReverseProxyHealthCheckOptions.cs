using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Options governing how often <see cref="PeriodicReverseProxyHealthChecker" /> polls
///     every active route's backend health.
/// </summary>
public sealed class PeriodicReverseProxyHealthCheckOptions
{
    /// <summary>
    ///     Gets the delay applied before the first health-check pass runs. Use a short value
    ///     to avoid blocking start-up; tests typically use <see cref="TimeSpan.Zero" /> for
    ///     determinism.
    /// </summary>
    public required TimeSpan InitialDelay { get; init; }

    /// <summary>
    ///     Gets the interval between successive health-check passes. Must be greater than
    ///     <see cref="TimeSpan.Zero" />.
    /// </summary>
    public required TimeSpan PollInterval { get; init; }
}
