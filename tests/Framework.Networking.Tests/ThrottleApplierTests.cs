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

    /// <summary>
    ///     Verifies that the download bandwidth applier completes immediately when the holder
    ///     is null.
    /// </summary>
    [Test]
    public async Task ApplyDownloadBandwidthAsync_NullHolder_CompletesImmediately()
    {
        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyDownloadBandwidthAsync(null, 1024, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that the download bandwidth applier completes immediately when the byte
    ///     count is zero.
    /// </summary>
    [Test]
    public async Task ApplyDownloadBandwidthAsync_ZeroBytes_CompletesImmediately()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("slow", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyDownloadBandwidthAsync(holder, 0, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that an unbounded download rate completes immediately even for large byte
    ///     counts.
    /// </summary>
    [Test]
    public async Task ApplyDownloadBandwidthAsync_UnboundedRate_CompletesImmediately()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = long.MaxValue,
            DownloadBytesPerSecond = long.MaxValue,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("unbounded", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyDownloadBandwidthAsync(holder, 1_000_000_000, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that a bounded download rate introduces a proportional delay.
    /// </summary>
    [Test]
    public async Task ApplyDownloadBandwidthAsync_PositiveBytesBoundedRate_DelaysProportionally()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("slow", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyDownloadBandwidthAsync(holder, 102, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsGreaterThanOrEqualTo(80);
    }

    /// <summary>
    ///     Verifies that the upload bandwidth applier completes immediately when the holder is
    ///     null.
    /// </summary>
    [Test]
    public async Task ApplyUploadBandwidthAsync_NullHolder_CompletesImmediately()
    {
        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyUploadBandwidthAsync(null, 1024, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that the upload bandwidth applier completes immediately when the byte
    ///     count is zero.
    /// </summary>
    [Test]
    public async Task ApplyUploadBandwidthAsync_ZeroBytes_CompletesImmediately()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("slow", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyUploadBandwidthAsync(holder, 0, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that an unbounded upload rate completes immediately even for large byte
    ///     counts.
    /// </summary>
    [Test]
    public async Task ApplyUploadBandwidthAsync_UnboundedRate_CompletesImmediately()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = long.MaxValue,
            DownloadBytesPerSecond = long.MaxValue,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("unbounded", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyUploadBandwidthAsync(holder, 1_000_000_000, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsLessThan(50);
    }

    /// <summary>
    ///     Verifies that a bounded upload rate introduces a proportional delay.
    /// </summary>
    [Test]
    public async Task ApplyUploadBandwidthAsync_PositiveBytesBoundedRate_DelaysProportionally()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("slow", parameters);
        var holder = new MutableThrottleProfile(profile);

        var start = DateTimeOffset.UtcNow;
        await ThrottleApplier.ApplyUploadBandwidthAsync(holder, 102, CancellationToken.None);
        var elapsed = DateTimeOffset.UtcNow - start;

        await Assert.That(elapsed.TotalMilliseconds).IsGreaterThanOrEqualTo(80);
    }

    /// <summary>
    ///     Verifies that the packet loss helper returns false when the holder is null.
    /// </summary>
    [Test]
    public async Task HasPacketLossOccurred_NullHolder_ReturnsFalse()
    {
        var result = ThrottleApplier.HasPacketLossOccurred(null, () => 0.0);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that the packet loss helper returns false when the configured probability
    ///     is zero.
    /// </summary>
    [Test]
    public async Task HasPacketLossOccurred_ZeroProbability_ReturnsFalse()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var profile = new ThrottleProfile("clean", parameters);
        var holder = new MutableThrottleProfile(profile);

        var result = ThrottleApplier.HasPacketLossOccurred(holder, () => 0.0);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that the packet loss helper returns true when the sampled value is below
    ///     the configured probability.
    /// </summary>
    [Test]
    public async Task HasPacketLossOccurred_SampleBelowProbability_ReturnsTrue()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0.5,
        };
        var profile = new ThrottleProfile("lossy", parameters);
        var holder = new MutableThrottleProfile(profile);

        var result = ThrottleApplier.HasPacketLossOccurred(holder, () => 0.1);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that the packet loss helper returns false when the sampled value is at or
    ///     above the configured probability.
    /// </summary>
    [Test]
    public async Task HasPacketLossOccurred_SampleAtOrAboveProbability_ReturnsFalse()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0.5,
        };
        var profile = new ThrottleProfile("lossy", parameters);
        var holder = new MutableThrottleProfile(profile);

        var result = ThrottleApplier.HasPacketLossOccurred(holder, () => 0.9);

        await Assert.That(result).IsFalse();
    }
}
