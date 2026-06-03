using System;

namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Immutable point-in-time snapshot of a single tracked remote device. Produced by
///     <see cref="RemoteDeviceTracker.Snapshot" /> so consumers can observe a stable view
///     of device state independent of later tracker mutations.
/// </summary>
public sealed record RemoteDeviceSnapshot
{
    /// <summary>
    ///     Gets the network address (IPv4 or IPv6 in textual form) of the device.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    ///     Gets the timestamp of the first request received from this device.
    /// </summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    ///     Gets the detected device kind at the time of the snapshot.
    /// </summary>
    public required RemoteDeviceKind Kind { get; init; }

    /// <summary>
    ///     Gets the timestamp of the most recent request received at the time of the snapshot.
    /// </summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>
    ///     Gets the human-friendly label for the device at the time of the snapshot.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Gets the total number of requests received from this device at the time of the snapshot.
    /// </summary>
    public required long RequestCount { get; init; }

    /// <summary>
    ///     Gets the device's status at the time of the snapshot.
    /// </summary>
    public required RemoteDeviceStatus Status { get; init; }

    /// <summary>
    ///     Gets the most recent User-Agent string observed at the time of the snapshot.
    /// </summary>
    public required string? UserAgent { get; init; }
}
