using System;

namespace Proxyfan.Domain.Throttling;

/// <summary>
///     Describes the bandwidth, latency, and packet-loss characteristics of a simulated network
///     connection for use by the throttling middleware.
/// </summary>
public sealed class ThrottleProfile
{
    /// <summary>
    ///     Gets the downstream bandwidth in bytes per second. <see cref="long.MaxValue" /> for unbounded.
    /// </summary>
    public long DownloadBytesPerSecond { get; }

    /// <summary>
    ///     Gets the simulated end-to-end latency added to every transfer.
    /// </summary>
    public TimeSpan Latency { get; }

    /// <summary>
    ///     Gets the profile's display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the packet-loss probability in the range 0.0 (no loss) to 1.0 (everything lost).
    /// </summary>
    public double PacketLossProbability { get; }

    /// <summary>
    ///     Gets the upstream bandwidth in bytes per second. <see cref="long.MaxValue" /> for unbounded.
    /// </summary>
    public long UploadBytesPerSecond { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfile" />.
    /// </summary>
    /// <param name="name">The profile's display name.</param>
    /// <param name="parameters">The profile parameters.</param>
    public ThrottleProfile(string name, ThrottleProfileParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must be provided.", nameof(name));
        }

        if (parameters.UploadBytesPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.UploadBytesPerSecond, "Upload rate must be positive.");
        }

        if (parameters.DownloadBytesPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.DownloadBytesPerSecond, "Download rate must be positive.");
        }

        if (!double.IsFinite(parameters.PacketLossProbability) || parameters.PacketLossProbability is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.PacketLossProbability, "Packet loss probability must be a finite value in [0, 1].");
        }

        if (parameters.Latency < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.Latency, "Latency must be non-negative.");
        }

        Name = name;
        UploadBytesPerSecond = parameters.UploadBytesPerSecond;
        DownloadBytesPerSecond = parameters.DownloadBytesPerSecond;
        Latency = parameters.Latency;
        PacketLossProbability = parameters.PacketLossProbability;
    }
}
