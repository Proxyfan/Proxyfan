using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.RemoteDevices;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="RemoteDevicesViewModel" />.
/// </summary>
public sealed class RemoteDevicesViewModelTests
{
    /// <summary>
    ///     After construction, the view model exposes the current devices.
    /// </summary>
    [Test]
    public async Task Constructor_PreexistingDevices_PopulatesCollection()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.1", "Mozilla/5.0");
        tracker.RecordRequest("10.0.0.2", "curl/8.0");

        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.Devices.Count).IsEqualTo(2);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The view model adds new rows when the tracker observes a new device.
    /// </summary>
    [Test]
    public async Task TrackerChanged_NewDevice_AddsItem()
    {
        var tracker = new RemoteDeviceTracker();
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);

        tracker.RecordRequest("10.0.0.5", "Android");

        await Assert.That(viewModel.Devices.Count).IsEqualTo(1);
        await Assert.That(viewModel.Devices[0].Address).IsEqualTo("10.0.0.5");
        viewModel.Dispose();
    }

    /// <summary>
    ///     Existing row instances are reused (and mutated) when the tracker reports
    ///     state changes for the same device.
    /// </summary>
    [Test]
    public async Task TrackerChanged_ExistingDevice_UpdatesRowInPlace()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.5", "Android");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);
        var originalRow = viewModel.Devices[0];

        tracker.RecordRequest("10.0.0.5", "Android");

        await Assert.That(viewModel.Devices.Count).IsEqualTo(1);
        await Assert.That(viewModel.Devices[0]).IsSameReferenceAs(originalRow);
        await Assert.That(viewModel.Devices[0].RequestCount).IsEqualTo(1L);
        viewModel.Dispose();
    }

    /// <summary>
    ///     Forgotten devices disappear from the collection.
    /// </summary>
    [Test]
    public async Task TrackerChanged_ForgottenDevice_RemovesRow()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.7", "iOS");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);

        tracker.Forget("10.0.0.7");

        await Assert.That(viewModel.Devices.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Disconnect command marks the selected device as disconnected on the tracker.
    /// </summary>
    [Test]
    public async Task DisconnectCommand_SelectedDevice_MarksDisconnected()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.1", "ua");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);
        var device = viewModel.Devices[0];

        viewModel.DisconnectCommand.Execute(device);

        var snapshot = tracker.Snapshot();
        await Assert.That(snapshot[0].Status).IsEqualTo(RemoteDeviceStatus.Disconnected);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Disconnect command with a null device is a safe no-op.
    /// </summary>
    [Test]
    public async Task DisconnectCommand_NullDevice_IsNoOp()
    {
        var tracker = new RemoteDeviceTracker();
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);

        viewModel.DisconnectCommand.Execute(null);

        await Assert.That(viewModel.Devices.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Forget command removes the selected device from the tracker.
    /// </summary>
    [Test]
    public async Task ForgetCommand_SelectedDevice_RemovesFromTracker()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.1", "ua");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);
        var device = viewModel.Devices[0];

        viewModel.ForgetCommand.Execute(device);

        await Assert.That(tracker.Snapshot().Count).IsEqualTo(0);
        await Assert.That(viewModel.Devices.Count).IsEqualTo(0);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Forget command with a null device is a safe no-op.
    /// </summary>
    [Test]
    public async Task ForgetCommand_NullDevice_IsNoOp()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.1", "ua");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);

        viewModel.ForgetCommand.Execute(null);

        await Assert.That(tracker.Snapshot().Count).IsEqualTo(1);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Rename command applies the rename text to the selected device and clears the editor.
    /// </summary>
    [Test]
    public async Task RenameCommand_ValidInput_RenamesSelectedDevice()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.1", "ua");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance)
        {
            SelectedDevice = null,
            RenameText = "Office Laptop",
        };
        viewModel.SelectedDevice = viewModel.Devices[0];

        viewModel.RenameCommand.Execute(null);

        await Assert.That(viewModel.Devices[0].Name).IsEqualTo("Office Laptop");
        await Assert.That(viewModel.RenameText).IsEqualTo(string.Empty);
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Rename command without a selection is a no-op.
    /// </summary>
    [Test]
    public async Task RenameCommand_NoSelection_IsNoOp()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.1", "ua");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance)
        {
            RenameText = "Office Laptop",
        };

        viewModel.RenameCommand.Execute(null);

        await Assert.That(viewModel.Devices[0].Name).IsEqualTo("10.0.0.1");
        await Assert.That(viewModel.RenameText).IsEqualTo("Office Laptop");
        viewModel.Dispose();
    }

    /// <summary>
    ///     The Rename command with empty text is a no-op.
    /// </summary>
    [Test]
    public async Task RenameCommand_EmptyText_IsNoOp()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.1", "ua");
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance)
        {
            SelectedDevice = null,
            RenameText = "  ",
        };
        viewModel.SelectedDevice = viewModel.Devices[0];

        viewModel.RenameCommand.Execute(null);

        await Assert.That(viewModel.Devices[0].Name).IsEqualTo("10.0.0.1");
        viewModel.Dispose();
    }

    /// <summary>
    ///     After disposal, subsequent tracker mutations do not update the view model.
    /// </summary>
    [Test]
    public async Task Dispose_AfterDispose_UnsubscribesFromChangedEvent()
    {
        var tracker = new RemoteDeviceTracker();
        var viewModel = new RemoteDevicesViewModel(tracker, InlineUserInterfaceScheduler.Instance);

        viewModel.Dispose();
        tracker.RecordRequest("10.0.0.99", "ua");

        await Assert.That(viewModel.Devices.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     The item view model's UpdateFrom refreshes every mutable property.
    /// </summary>
    [Test]
    public async Task ItemViewModel_UpdateFrom_RefreshesObservableProperties()
    {
        var initial = new RemoteDeviceInfo("10.0.0.1", DateTimeOffset.UnixEpoch, "Mozilla/5.0");
        var item = new RemoteDeviceItemViewModel(initial);

        initial.RecordRequest(DateTimeOffset.UnixEpoch.AddSeconds(5), "iOS");
        initial.Rename("My Phone");
        item.UpdateFrom(initial);

        await Assert.That(item.Name).IsEqualTo("My Phone");
        await Assert.That(item.RequestCount).IsEqualTo(1L);
        await Assert.That(item.UserAgent).IsEqualTo("iOS");
    }
}
