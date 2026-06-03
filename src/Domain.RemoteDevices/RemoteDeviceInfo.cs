using System;

namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Live state about a single remote device connected through the proxy.
///     Mutable; updated by <see cref="RemoteDeviceTracker" />.
/// </summary>
public sealed class RemoteDeviceInfo
{
    /// <summary>
    ///     Gets the network address (IPv4 or IPv6 in textual form) of the device.
    /// </summary>
    public string Address { get; }

    /// <summary>
    ///     Gets the timestamp of the first request received from this device.
    /// </summary>
    public DateTimeOffset FirstSeen { get; }

    /// <summary>
    ///     Gets the detected device kind.
    /// </summary>
    public RemoteDeviceKind Kind { get; private set; }

    /// <summary>
    ///     Gets the timestamp of the most recent request received from this device.
    /// </summary>
    public DateTimeOffset LastSeen { get; private set; }

    /// <summary>
    ///     Gets the human-friendly label for the device. Defaults to the device address
    ///     when the user has not assigned a custom name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///     Gets the total number of requests received from this device since it was tracked.
    /// </summary>
    public long RequestCount { get; private set; }

    /// <summary>
    ///     Gets the device's current status.
    /// </summary>
    public RemoteDeviceStatus Status { get; private set; }

    /// <summary>
    ///     Gets the most recent User-Agent string observed from this device.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteDeviceInfo" />.
    /// </summary>
    /// <param name="address">The network address of the device. Must be non-empty.</param>
    /// <param name="firstSeen">The timestamp the device first issued a request.</param>
    /// <param name="userAgent">The initial User-Agent string, or null when unavailable.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="address" /> is empty or whitespace.
    /// </exception>
    public RemoteDeviceInfo(string address, DateTimeOffset firstSeen, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address must not be empty or whitespace.", nameof(address));
        }

        Address = address;
        FirstSeen = firstSeen;
        LastSeen = firstSeen;
        Name = address;
        UserAgent = userAgent;
        Kind = RemoteDeviceUserAgentClassifier.Classify(userAgent);
        RequestCount = 0;
        Status = RemoteDeviceStatus.Active;
    }

    /// <summary>
    ///     Marks the device as disconnected and freezes its state.
    /// </summary>
    public void MarkDisconnected()
    {
        Status = RemoteDeviceStatus.Disconnected;
    }

    /// <summary>
    ///     Marks the device as idle (no active requests within the idle window).
    /// </summary>
    public void MarkIdle()
    {
        if (Status == RemoteDeviceStatus.Active)
        {
            Status = RemoteDeviceStatus.Idle;
        }
    }

    /// <summary>
    ///     Records a request from the device.
    /// </summary>
    /// <param name="timestamp">The time the request arrived.</param>
    /// <param name="userAgent">The request's User-Agent header, or null when missing.</param>
    public void RecordRequest(DateTimeOffset timestamp, string? userAgent)
    {
        LastSeen = timestamp;
        RequestCount++;
        Status = RemoteDeviceStatus.Active;
        if (!string.IsNullOrWhiteSpace(userAgent) && !string.Equals(UserAgent, userAgent, StringComparison.Ordinal))
        {
            UserAgent = userAgent;
            Kind = RemoteDeviceUserAgentClassifier.Classify(userAgent);
        }
    }

    /// <summary>
    ///     Replaces the user-visible label.
    /// </summary>
    /// <param name="name">The new label. Must be non-empty.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name" /> is empty or whitespace.
    /// </exception>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty or whitespace.", nameof(name));
        }

        Name = name;
    }

    /// <summary>
    ///     Returns an immutable point-in-time copy of this device's current state.
    /// </summary>
    /// <returns>A new <see cref="RemoteDeviceSnapshot" /> capturing the current values.</returns>
    public RemoteDeviceSnapshot ToSnapshot()
    {
        return new RemoteDeviceSnapshot
        {
            Address = Address,
            FirstSeen = FirstSeen,
            Kind = Kind,
            LastSeen = LastSeen,
            Name = Name,
            RequestCount = RequestCount,
            Status = Status,
            UserAgent = UserAgent,
        };
    }
}
