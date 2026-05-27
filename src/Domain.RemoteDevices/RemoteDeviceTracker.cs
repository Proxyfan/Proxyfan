using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Thread-safe registry of remote devices connected through the proxy. Tracks
///     first-/last-seen timestamps, request counts, and status transitions
///     (active → idle → disconnected) driven by a configurable idle threshold.
/// </summary>
public sealed class RemoteDeviceTracker
{
    /// <summary>
    ///     Raised after any change to the device collection or any tracked device's state.
    /// </summary>
    public event RemoteDeviceTrackerChanged? Changed;

    /// <summary>
    ///     The default disconnection threshold (5 minutes) before an idle device is marked disconnected.
    /// </summary>
    public static readonly TimeSpan DefaultDisconnectThreshold;

    /// <summary>
    ///     The default idle threshold (60 seconds) before an active device transitions to idle.
    /// </summary>
    public static readonly TimeSpan DefaultIdleThreshold;
    private readonly Dictionary<string, RemoteDeviceInfo> _devices;
    private readonly TimeSpan _disconnectThreshold;
    private readonly TimeSpan _idleThreshold;
    private readonly Lock _lock;
    private readonly TimeProvider _timeProvider;

    static RemoteDeviceTracker()
    {
        DefaultIdleThreshold = TimeSpan.FromSeconds(60);
        DefaultDisconnectThreshold = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    ///     Initializes a new <see cref="RemoteDeviceTracker" /> with default thresholds and the
    ///     system time provider.
    /// </summary>
    public RemoteDeviceTracker()
        : this(TimeProvider.System, DefaultIdleThreshold, DefaultDisconnectThreshold)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="RemoteDeviceTracker" /> using default idle and
    ///     disconnect thresholds but an explicit time provider.
    /// </summary>
    /// <param name="timeProvider">The time provider used to derive timestamps and run transitions.</param>
    public RemoteDeviceTracker(TimeProvider timeProvider)
        : this(timeProvider, DefaultIdleThreshold, DefaultDisconnectThreshold)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="RemoteDeviceTracker" />.
    /// </summary>
    /// <param name="timeProvider">The time provider used to derive timestamps and run transitions.</param>
    /// <param name="idleThreshold">The duration after which an active device becomes idle.</param>
    /// <param name="disconnectThreshold">
    ///     The duration after which an idle device is automatically marked disconnected.
    /// </param>
    public RemoteDeviceTracker(
        TimeProvider timeProvider,
        TimeSpan idleThreshold,
        TimeSpan disconnectThreshold)
    {
        _timeProvider = timeProvider;
        _idleThreshold = idleThreshold;
        _disconnectThreshold = disconnectThreshold;
        var devices = new Dictionary<string, RemoteDeviceInfo>(StringComparer.OrdinalIgnoreCase);
        _devices = devices;
        var sync = new Lock();
        _lock = sync;
    }

    /// <summary>
    ///     Marks the device with the supplied <paramref name="address" /> as disconnected.
    ///     Does nothing when the device is unknown.
    /// </summary>
    /// <param name="address">The device address.</param>
    public void Disconnect(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var changed = false;
        lock (_lock)
        {
            if (_devices.TryGetValue(address, out var device) && device.Status != RemoteDeviceStatus.Disconnected)
            {
                device.MarkDisconnected();
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this);
        }
    }

    /// <summary>
    ///     Permanently removes the device with the supplied <paramref name="address" /> from
    ///     the registry. Does nothing when the device is unknown.
    /// </summary>
    /// <param name="address">The device address.</param>
    public void Forget(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var changed = false;
        lock (_lock)
        {
            if (_devices.Remove(address))
            {
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this);
        }
    }

    /// <summary>
    ///     Records a request from the supplied <paramref name="address" />. Adds the device
    ///     when first seen, otherwise increments its counters.
    /// </summary>
    /// <param name="address">The device address.</param>
    /// <param name="userAgent">The request's User-Agent, or null when absent.</param>
    /// <returns>The updated <see cref="RemoteDeviceInfo" />.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="address" /> is empty or whitespace.
    /// </exception>
    public RemoteDeviceInfo RecordRequest(string address, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address must not be empty or whitespace.", nameof(address));
        }

        var now = _timeProvider.GetUtcNow();
        RemoteDeviceInfo info;
        lock (_lock)
        {
            if (!_devices.TryGetValue(address, out var existing))
            {
                var newInfo = new RemoteDeviceInfo(address, now, userAgent);
                _devices[address] = newInfo;
                info = newInfo;
            }
            else
            {
                existing.RecordRequest(now, userAgent);
                info = existing;
            }
        }

        Changed?.Invoke(this);
        return info;
    }

    /// <summary>
    ///     Renames the device with the supplied <paramref name="address" />.
    /// </summary>
    /// <param name="address">The device address.</param>
    /// <param name="name">The new label.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name" /> is empty or whitespace.
    /// </exception>
    public void Rename(string address, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty or whitespace.", nameof(name));
        }

        var changed = false;
        lock (_lock)
        {
            if (_devices.TryGetValue(address, out var device))
            {
                device.Rename(name);
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke(this);
        }
    }

    /// <summary>
    ///     Returns a read-only snapshot of the current devices in insertion order.
    /// </summary>
    /// <returns>A new read-only collection.</returns>
    public ReadOnlyCollection<RemoteDeviceInfo> Snapshot()
    {
        lock (_lock)
        {
            var array = new RemoteDeviceInfo[_devices.Count];
            var index = 0;
            foreach (var entry in _devices)
            {
                array[index] = entry.Value;
                index++;
            }

            var snapshot = new ReadOnlyCollection<RemoteDeviceInfo>(array);
            return snapshot;
        }
    }

    /// <summary>
    ///     Applies idle and disconnection transitions based on the current time. Active
    ///     devices last seen longer than the idle threshold are marked
    ///     <see cref="RemoteDeviceStatus.Idle" />; idle devices last seen longer than
    ///     the disconnect threshold are marked
    ///     <see cref="RemoteDeviceStatus.Disconnected" />.
    /// </summary>
    public void Tick()
    {
        var now = _timeProvider.GetUtcNow();
        var changed = false;
        lock (_lock)
        {
            foreach (var entry in _devices)
            {
                var device = entry.Value;
                var elapsed = now - device.LastSeen;
                if (device.Status == RemoteDeviceStatus.Active && elapsed >= _idleThreshold)
                {
                    device.MarkIdle();
                    changed = true;
                    continue;
                }

                if (device.Status == RemoteDeviceStatus.Idle && elapsed >= _disconnectThreshold)
                {
                    device.MarkDisconnected();
                    changed = true;
                }
            }
        }

        if (changed)
        {
            Changed?.Invoke(this);
        }
    }
}
