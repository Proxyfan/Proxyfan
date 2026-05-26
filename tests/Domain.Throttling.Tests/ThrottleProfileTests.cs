using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Throttling.Tests;

/// <summary>
///     Tests for <see cref="ThrottleProfile" />.
/// </summary>
public sealed class ThrottleProfileTests
{
    /// <summary>
    ///     Verifies that the constructor stores all supplied values.
    /// </summary>
    [Test]
    public async Task Constructor_WithValidParameters_StoresValues()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 2048,
            Latency = TimeSpan.FromMilliseconds(50),
            PacketLossProbability = 0.01,
        };

        var profile = new ThrottleProfile("My Profile", parameters);

        await Assert.That(profile.Name).IsEqualTo("My Profile");
        await Assert.That(profile.UploadBytesPerSecond).IsEqualTo(1024L);
        await Assert.That(profile.DownloadBytesPerSecond).IsEqualTo(2048L);
        await Assert.That(profile.Latency).IsEqualTo(TimeSpan.FromMilliseconds(50));
        await Assert.That(profile.PacketLossProbability).IsEqualTo(0.01);
    }

    /// <summary>
    ///     Verifies that an empty name throws.
    /// </summary>
    [Test]
    public async Task Constructor_WithEmptyName_Throws()
    {
        var parameters = CreateValidParameters();

        await Assert.That(() => _ = new ThrottleProfile(string.Empty, parameters)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that a zero upload rate throws.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroUploadRate_Throws()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 0,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };

        await Assert.That(() => _ = new ThrottleProfile("p", parameters)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that a zero download rate throws.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroDownloadRate_Throws()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 0,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };

        await Assert.That(() => _ = new ThrottleProfile("p", parameters)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that a negative latency throws.
    /// </summary>
    [Test]
    public async Task Constructor_WithNegativeLatency_Throws()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.FromSeconds(-1),
            PacketLossProbability = 0,
        };

        await Assert.That(() => _ = new ThrottleProfile("p", parameters)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that a packet-loss probability above 1 throws.
    /// </summary>
    [Test]
    public async Task Constructor_WithPacketLossAboveRange_Throws()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 1.5,
        };

        await Assert.That(() => _ = new ThrottleProfile("p", parameters)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies that a negative packet-loss probability throws.
    /// </summary>
    [Test]
    public async Task Constructor_WithPacketLossBelowRange_Throws()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = -0.1,
        };

        await Assert.That(() => _ = new ThrottleProfile("p", parameters)).Throws<ArgumentOutOfRangeException>();
    }

    private static ThrottleProfileParameters CreateValidParameters()
    {
        return new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 0,
        };
    }
}
