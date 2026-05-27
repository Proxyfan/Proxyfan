using Proxyfan.Domain.Throttling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ThrottleApplier" />.
/// </summary>
public sealed class ThrottleApplierTests
{
    /// <summary>
    ///     Verifies that a null holder completes immediately without throwing.
    /// </summary>
    [Test]
    public async Task ApplyLatencyAsync_NullHolder_CompletesImmediately()
    {
        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyLatencyAsync(null, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that an empty holder (no active profile) completes immediately.
    /// </summary>
    [Test]
    public async Task ApplyLatencyAsync_HolderWithNoProfile_CompletesImmediately()
    {
        var holder = new MutableThrottleProfile();

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyLatencyAsync(holder, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that a profile with zero latency completes immediately.
    /// </summary>
    [Test]
    public async Task ApplyLatencyAsync_ZeroLatency_CompletesImmediately()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("test", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyLatencyAsync(holder, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that a non-zero latency profile introduces at least the configured delay.
    /// </summary>
    [Test]
    public async Task ApplyLatencyAsync_PositiveLatency_DelaysAtLeastConfiguredDuration()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.FromMilliseconds(50),
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("test", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyLatencyAsync(holder, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsGreaterThanOrEqualTo(40);
    }
}
