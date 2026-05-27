using Proxyfan.Domain.Throttling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Applies the configured throttle profile to network operations. Currently honours the
///     <see cref="ThrottleProfile.Latency" /> field by introducing a delay before delivery.
///     Bandwidth and packet loss are recorded but not yet enforced.
/// </summary>
public static class ThrottleApplier
{
    /// <summary>
    ///     Applies the latency portion of the supplied throttle profile by delaying for the
    ///     configured duration. Returns immediately when the holder is null, when no profile
    ///     is active, or when latency is zero.
    /// </summary>
    /// <param name="throttle">The mutable holder that exposes the active profile, or <see langword="null" />.</param>
    /// <param name="cancellationToken">A token that cancels the delay.</param>
    /// <returns>A task that completes once the latency has elapsed.</returns>
    public static async Task ApplyLatencyAsync(MutableThrottleProfile? throttle, CancellationToken cancellationToken)
    {
        var profile = throttle?.Profile;

        if (profile is null || profile.Latency <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(profile.Latency, cancellationToken).ConfigureAwait(false);
    }
}
