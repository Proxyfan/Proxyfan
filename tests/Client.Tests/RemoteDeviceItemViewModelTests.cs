using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.RemoteDevices;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="RemoteDeviceItemViewModel" />.
/// </summary>
public sealed class RemoteDeviceItemViewModelTests
{
    [Test]
    public async Task Constructor_FromDevice_CopiesAllProperties()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.5", "Mozilla/5.0");
        var device = tracker.Snapshot()[0];

        var viewModel = new RemoteDeviceItemViewModel(device);

        await Assert.That(viewModel.Address).IsEqualTo("10.0.0.5");
        await Assert.That(viewModel.Name).IsEqualTo("10.0.0.5");
        await Assert.That(viewModel.UserAgent).IsEqualTo("Mozilla/5.0");
        await Assert.That(viewModel.Status).IsEqualTo(RemoteDeviceStatus.Active);
        await Assert.That(viewModel.RequestCount).IsEqualTo(0L);
    }

    [Test]
    public async Task UpdateFrom_DeviceWithNewState_RefreshesObservableProperties()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.5", null);
        var viewModel = new RemoteDeviceItemViewModel(tracker.Snapshot()[0]);
        tracker.RecordRequest("10.0.0.5", "curl/8.0");
        tracker.RecordRequest("10.0.0.5", "curl/8.0");
        tracker.Rename("10.0.0.5", "My Laptop");

        viewModel.UpdateFrom(tracker.Snapshot()[0]);

        await Assert.That(viewModel.RequestCount).IsEqualTo(2L);
        await Assert.That(viewModel.UserAgent).IsEqualTo("curl/8.0");
        await Assert.That(viewModel.Name).IsEqualTo("My Laptop");
        await Assert.That(viewModel.Status).IsEqualTo(RemoteDeviceStatus.Active);
    }

    [Test]
    public async Task UpdateFrom_DisconnectedDevice_ReflectsStatusChange()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.5", null);
        var viewModel = new RemoteDeviceItemViewModel(tracker.Snapshot()[0]);
        tracker.Disconnect("10.0.0.5");

        viewModel.UpdateFrom(tracker.Snapshot()[0]);

        await Assert.That(viewModel.Status).IsEqualTo(RemoteDeviceStatus.Disconnected);
    }
}
