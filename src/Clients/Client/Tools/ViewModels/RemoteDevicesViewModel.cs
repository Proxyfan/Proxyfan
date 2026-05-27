using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.RemoteDevices;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Remote Devices tool window. Subscribes to a
///     <see cref="RemoteDeviceTracker" /> and projects each tracked device into an
///     observable item view model. Supports selection, disconnection, removal, and
///     renaming.
/// </summary>
public sealed partial class RemoteDevicesViewModel : ObservableObject, IDisposable
{
    private readonly Dictionary<string, RemoteDeviceItemViewModel> _items;
    private readonly RemoteDeviceTracker _tracker;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _renameText;
    [ObservableProperty]
    private RemoteDeviceItemViewModel? _selectedDevice;

    /// <summary>
    ///     Gets the current devices known to the tracker.
    /// </summary>
    public ObservableCollection<RemoteDeviceItemViewModel> Devices { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteDevicesViewModel" />.
    /// </summary>
    /// <param name="tracker">The tracker that publishes device updates.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public RemoteDevicesViewModel(RemoteDeviceTracker tracker, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _tracker = tracker;
        _userInterfaceScheduler = userInterfaceScheduler;
        var items = new Dictionary<string, RemoteDeviceItemViewModel>(StringComparer.OrdinalIgnoreCase);
        _items = items;
        _renameText = string.Empty;
        Devices = [];
        _tracker.Changed += OnTrackerChanged;
        ReloadDevices();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tracker.Changed -= OnTrackerChanged;
    }

    [RelayCommand]
    private void Disconnect(RemoteDeviceItemViewModel? device)
    {
        if (device is null)
        {
            return;
        }

        _tracker.Disconnect(device.Address);
    }

    [RelayCommand]
    private void Forget(RemoteDeviceItemViewModel? device)
    {
        if (device is null)
        {
            return;
        }

        _tracker.Forget(device.Address);
    }

    private void OnTrackerChanged(RemoteDeviceTracker tracker)
    {
        _userInterfaceScheduler.Post(ReloadDevices);
    }

    private void ReloadDevices()
    {
        var snapshot = _tracker.Snapshot();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var device in snapshot)
        {
            seen.Add(device.Address);
            if (_items.TryGetValue(device.Address, out var existing))
            {
                existing.UpdateFrom(device);
            }
            else
            {
                var item = new RemoteDeviceItemViewModel(device);
                _items[device.Address] = item;
                Devices.Add(item);
            }
        }

        for (var index = Devices.Count - 1; index >= 0; index--)
        {
            var current = Devices[index];
            if (!seen.Contains(current.Address))
            {
                Devices.RemoveAt(index);
                _items.Remove(current.Address);
            }
        }
    }

    [RelayCommand]
    private void Rename()
    {
        var target = SelectedDevice;
        if (target is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(RenameText))
        {
            return;
        }

        _tracker.Rename(target.Address, RenameText);
        RenameText = string.Empty;
    }
}
