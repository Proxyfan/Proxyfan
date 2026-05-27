using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.RemoteDevices;
using System;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model wrapper around a single <see cref="RemoteDeviceInfo" /> for display in
///     the Remote Devices tool window. Properties mirror the underlying model and are
///     refreshed by the parent <see cref="RemoteDevicesViewModel" /> when the tracker
///     reports changes.
/// </summary>
public sealed partial class RemoteDeviceItemViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset _firstSeen;
    [ObservableProperty]
    private RemoteDeviceKind _kind;
    [ObservableProperty]
    private DateTimeOffset _lastSeen;
    [ObservableProperty]
    private string _name;
    [ObservableProperty]
    private long _requestCount;
    [ObservableProperty]
    private RemoteDeviceStatus _status;
    [ObservableProperty]
    private string? _userAgent;

    /// <summary>
    ///     Gets the device's stable network address (key for the tracker).
    /// </summary>
    public string Address { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteDeviceItemViewModel" /> from a domain device record.
    /// </summary>
    /// <param name="device">The underlying device record.</param>
    public RemoteDeviceItemViewModel(RemoteDeviceInfo device)
    {
        Address = device.Address;
        _firstSeen = device.FirstSeen;
        _kind = device.Kind;
        _lastSeen = device.LastSeen;
        _name = device.Name;
        _requestCount = device.RequestCount;
        _status = device.Status;
        _userAgent = device.UserAgent;
    }

    /// <summary>
    ///     Refreshes the observable properties from the supplied device record so existing
    ///     bindings observe the change without rebuilding the row.
    /// </summary>
    /// <param name="device">The updated device record.</param>
    public void UpdateFrom(RemoteDeviceInfo device)
    {
        Kind = device.Kind;
        LastSeen = device.LastSeen;
        Name = device.Name;
        RequestCount = device.RequestCount;
        Status = device.Status;
        UserAgent = device.UserAgent;
    }
}
