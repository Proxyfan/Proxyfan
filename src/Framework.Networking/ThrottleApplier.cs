using Microsoft.Extensions.Options;
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
    ///     configured duration. Returns immediately when the profile is null, when bypassed,
    ///     or when latency is zero.
    /// </summary>
    /// <param name="profileMonitor">The options monitor that resolves the current profile.</param>
    /// <param name="cancellationToken">A token that cancels the delay.</param>
    /// <returns>A task that completes once the latency has elapsed.</returns>
    public static async Task ApplyLatencyAsync(IOptionsMonitor<ThrottleProfile>? profileMonitor, CancellationToken cancellationToken)
    {
        var profile = profileMonitor?.CurrentValue;

        if (profile is null || profile.Latency <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(profile.Latency, cancellationToken).ConfigureAwait(false);
    }
}
