using System;

namespace Proxyfan.Domain.Throttling;

/// <summary>
///     Library of well-known <see cref="ThrottleProfile" /> presets covering common mobile and
///     fixed-network conditions for QA testing.
/// </summary>
public static class ThrottleProfilePresets
{
    /// <summary>
    ///     Gets a profile simulating "Bad" (slow + lossy) network conditions.
    /// </summary>
    /// <returns>A configured throttle profile.</returns>
    public static ThrottleProfile BadNetwork()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 64 * 1024,
            DownloadBytesPerSecond = 256 * 1024,
            Latency = TimeSpan.FromMilliseconds(500),
            PacketLossProbability = 0.05,
        };
        return new ThrottleProfile("Bad Network", parameters);
    }

    /// <summary>
    ///     Gets a profile simulating 100% packet loss (no traffic can be delivered).
    /// </summary>
    /// <returns>A configured throttle profile.</returns>
    public static ThrottleProfile CompleteLoss()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1024,
            DownloadBytesPerSecond = 1024,
            Latency = TimeSpan.Zero,
            PacketLossProbability = 1.0,
        };
        return new ThrottleProfile("100% Loss", parameters);
    }

    /// <summary>
    ///     Gets a profile simulating fast 4G LTE.
    /// </summary>
    /// <returns>A configured throttle profile.</returns>
    public static ThrottleProfile FastFourthGeneration()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 9 * 1024 * 1024,
            DownloadBytesPerSecond = 12 * 1024 * 1024,
            Latency = TimeSpan.FromMilliseconds(20),
            PacketLossProbability = 0,
        };
        return new ThrottleProfile("4G", parameters);
    }

    /// <summary>
    ///     Gets a profile simulating Edge / 2G.
    /// </summary>
    /// <returns>A configured throttle profile.</returns>
    public static ThrottleProfile SlowSecondGeneration()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 32 * 1024,
            DownloadBytesPerSecond = 64 * 1024,
            Latency = TimeSpan.FromMilliseconds(300),
            PacketLossProbability = 0,
        };
        return new ThrottleProfile("2G", parameters);
    }

    /// <summary>
    ///     Gets a profile simulating 3G.
    /// </summary>
    /// <returns>A configured throttle profile.</returns>
    public static ThrottleProfile ThirdGeneration()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 128 * 1024,
            DownloadBytesPerSecond = 384 * 1024,
            Latency = TimeSpan.FromMilliseconds(100),
            PacketLossProbability = 0,
        };
        return new ThrottleProfile("3G", parameters);
    }

    /// <summary>
    ///     Gets a profile simulating WiFi (high bandwidth, low latency).
    /// </summary>
    /// <returns>A configured throttle profile.</returns>
    public static ThrottleProfile Wireless()
    {
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 30 * 1024 * 1024,
            DownloadBytesPerSecond = 30 * 1024 * 1024,
            Latency = TimeSpan.FromMilliseconds(2),
            PacketLossProbability = 0,
        };
        return new ThrottleProfile("WiFi", parameters);
    }
}
