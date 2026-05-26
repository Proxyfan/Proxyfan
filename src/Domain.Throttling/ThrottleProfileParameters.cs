using System;

namespace Proxyfan.Domain.Throttling;

/// <summary>
///     Parameters that configure a <see cref="ThrottleProfile" />.
/// </summary>
public sealed class ThrottleProfileParameters
{
    /// <summary>
    ///     Gets the downstream bandwidth in bytes per second.
    /// </summary>
    public required long DownloadBytesPerSecond { get; init; }

    /// <summary>
    ///     Gets the simulated end-to-end latency added to every transfer.
    /// </summary>
    public required TimeSpan Latency { get; init; }

    /// <summary>
    ///     Gets the packet-loss probability in the range 0.0 to 1.0.
    /// </summary>
    public required double PacketLossProbability { get; init; }

    /// <summary>
    ///     Gets the upstream bandwidth in bytes per second.
    /// </summary>
    public required long UploadBytesPerSecond { get; init; }
}
