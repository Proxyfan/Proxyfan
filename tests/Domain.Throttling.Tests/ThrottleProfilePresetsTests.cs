using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Throttling.Tests;

/// <summary>
///     Tests for <see cref="ThrottleProfilePresets" />.
/// </summary>
public sealed class ThrottleProfilePresetsTests
{
    /// <summary>
    ///     Verifies that <see cref="ThrottleProfilePresets.SlowSecondGeneration" /> returns a 2G profile
    ///     with low bandwidth and high latency.
    /// </summary>
    [Test]
    public async Task SlowSecondGeneration_WhenCalled_ReturnsLowBandwidthProfile()
    {
        var profile = ThrottleProfilePresets.SlowSecondGeneration();

        await Assert.That(profile.Name).IsEqualTo("2G");
        await Assert.That(profile.UploadBytesPerSecond).IsLessThan(profile.DownloadBytesPerSecond);
        await Assert.That(profile.Latency).IsGreaterThan(TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfilePresets.ThirdGeneration" /> returns a 3G profile.
    /// </summary>
    [Test]
    public async Task ThirdGeneration_WhenCalled_ReturnsValidProfile()
    {
        var profile = ThrottleProfilePresets.ThirdGeneration();

        await Assert.That(profile.Name).IsEqualTo("3G");
        await Assert.That(profile.DownloadBytesPerSecond).IsGreaterThan(0L);
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfilePresets.FastFourthGeneration" /> returns a high-bandwidth 4G profile.
    /// </summary>
    [Test]
    public async Task FastFourthGeneration_WhenCalled_ReturnsHighBandwidthProfile()
    {
        var profile = ThrottleProfilePresets.FastFourthGeneration();

        await Assert.That(profile.Name).IsEqualTo("4G");
        await Assert.That(profile.DownloadBytesPerSecond).IsGreaterThan(1L * 1024 * 1024);
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfilePresets.Wireless" /> returns a low-latency WiFi profile.
    /// </summary>
    [Test]
    public async Task Wireless_WhenCalled_ReturnsLowLatencyProfile()
    {
        var profile = ThrottleProfilePresets.Wireless();

        await Assert.That(profile.Name).IsEqualTo("WiFi");
        await Assert.That(profile.Latency).IsLessThan(TimeSpan.FromMilliseconds(50));
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfilePresets.BadNetwork" /> returns a profile with
    ///     non-zero packet-loss probability.
    /// </summary>
    [Test]
    public async Task BadNetwork_WhenCalled_ReturnsProfileWithPacketLoss()
    {
        var profile = ThrottleProfilePresets.BadNetwork();

        await Assert.That(profile.Name).IsEqualTo("Bad Network");
        await Assert.That(profile.PacketLossProbability).IsGreaterThan(0.0);
    }

    /// <summary>
    ///     Verifies that <see cref="ThrottleProfilePresets.CompleteLoss" /> returns a profile with
    ///     100% packet loss.
    /// </summary>
    [Test]
    public async Task CompleteLoss_WhenCalled_Returns100PercentLossProfile()
    {
        var profile = ThrottleProfilePresets.CompleteLoss();

        await Assert.That(profile.Name).IsEqualTo("100% Loss");
        await Assert.That(profile.PacketLossProbability).IsEqualTo(1.0);
    }
}
