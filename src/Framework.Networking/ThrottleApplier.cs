using Proxyfan.Domain.Throttling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Applies the configured throttle profile to network operations. Honours the
///     <see cref="ThrottleProfile.Latency" /> field by introducing a delay before the response
///     is delivered, the <see cref="ThrottleProfile.DownloadBytesPerSecond" /> /
///     <see cref="ThrottleProfile.UploadBytesPerSecond" /> fields by introducing a transfer
///     delay proportional to the byte count being shipped through the proxy, and the
///     <see cref="ThrottleProfile.PacketLossProbability" /> field by reporting whether the
///     proxy should drop the current connection to simulate loss.
/// </summary>
public static class ThrottleApplier
{
    private const long UnboundedBandwidthThresholdBytesPerSecond = 1L << 50;

    /// <summary>
    ///     Applies a bandwidth-induced delay proportional to the supplied byte count and the
    ///     active profile's <see cref="ThrottleProfile.DownloadBytesPerSecond" /> rate. Returns
    ///     immediately when the holder is null, when no profile is active, when the byte count
    ///     is non-positive, or when the configured rate is effectively unbounded.
    /// </summary>
    /// <param name="throttle">The mutable holder that exposes the active profile, or <see langword="null" />.</param>
    /// <param name="byteCount">The number of bytes about to be transferred to the client.</param>
    /// <param name="cancellationToken">A token that cancels the delay.</param>
    /// <returns>A task that completes once the simulated bandwidth delay has elapsed.</returns>
    public static async Task ApplyDownloadBandwidthAsync(MutableThrottleProfile? throttle, long byteCount, CancellationToken cancellationToken)
    {
        var profile = throttle?.Profile;

        if (profile is null || byteCount <= 0 || profile.DownloadBytesPerSecond >= UnboundedBandwidthThresholdBytesPerSecond)
        {
            return;
        }

        var seconds = (double)byteCount / profile.DownloadBytesPerSecond;
        var delay = TimeSpan.FromSeconds(seconds);
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

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

    /// <summary>
    ///     Applies a bandwidth-induced delay proportional to the supplied byte count and the
    ///     active profile's <see cref="ThrottleProfile.UploadBytesPerSecond" /> rate. Returns
    ///     immediately when the holder is null, when no profile is active, when the byte count
    ///     is non-positive, or when the configured rate is effectively unbounded.
    /// </summary>
    /// <param name="throttle">The mutable holder that exposes the active profile, or <see langword="null" />.</param>
    /// <param name="byteCount">The number of bytes about to be transferred to the upstream origin.</param>
    /// <param name="cancellationToken">A token that cancels the delay.</param>
    /// <returns>A task that completes once the simulated bandwidth delay has elapsed.</returns>
    public static async Task ApplyUploadBandwidthAsync(MutableThrottleProfile? throttle, long byteCount, CancellationToken cancellationToken)
    {
        var profile = throttle?.Profile;

        if (profile is null || byteCount <= 0 || profile.UploadBytesPerSecond >= UnboundedBandwidthThresholdBytesPerSecond)
        {
            return;
        }

        var seconds = (double)byteCount / profile.UploadBytesPerSecond;
        var delay = TimeSpan.FromSeconds(seconds);
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns whether the proxy should drop the current operation to simulate packet loss
    ///     for the active profile. Returns <see langword="false" /> when the holder is null,
    ///     when no profile is active, or when the configured probability is zero. Uses the
    ///     supplied sampler for determinism in tests.
    /// </summary>
    /// <param name="throttle">The mutable holder that exposes the active profile, or <see langword="null" />.</param>
    /// <param name="sampler">A sampler that returns a uniform random value in <c>[0, 1)</c>.</param>
    /// <returns><see langword="true" /> when the operation should be dropped.</returns>
    public static bool HasPacketLossOccurred(MutableThrottleProfile? throttle, PacketLossSampler sampler)
    {
        var profile = throttle?.Profile;

        if (profile is null || profile.PacketLossProbability <= 0)
        {
            return false;
        }

        return sampler() < profile.PacketLossProbability;
    }
}
