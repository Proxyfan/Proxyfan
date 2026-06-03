using System;
using System.Threading.Tasks;
using Proxyfan.Domain.RemoteDevices;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.RemoteDevices.Tests;

public sealed class RemoteDeviceTrackerTests
{
    [Test]
    public async Task RecordRequest_FirstRequest_AddsDevice()
    {
        var tracker = new RemoteDeviceTracker();
        var info = tracker.RecordRequest("192.168.0.10", "Mozilla/5.0 (iPhone)");
        await Assert.That(info.Address).IsEqualTo("192.168.0.10");
        await Assert.That(tracker.Snapshot().Count).IsEqualTo(1);
    }

    [Test]
    public async Task RecordRequest_SecondRequest_IncrementsExistingDevice()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("192.168.0.10", "Mozilla/5.0 (iPhone)");
        var info = tracker.RecordRequest("192.168.0.10", "Mozilla/5.0 (iPhone)");
        await Assert.That(tracker.Snapshot().Count).IsEqualTo(1);
        await Assert.That(info.RequestCount).IsEqualTo(1L);
    }

    [Test]
    public async Task RecordRequest_EmptyAddress_Throws()
    {
        var tracker = new RemoteDeviceTracker();
        await Assert
            .That(() => tracker.RecordRequest(string.Empty, null))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task RecordRequest_ValidRequest_RaisesChanged()
    {
        var tracker = new RemoteDeviceTracker();
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.RecordRequest("10.0.0.1", null);
        tracker.RecordRequest("10.0.0.1", null);
        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task Disconnect_KnownDevice_SetsStatusAndRaisesChanged()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("192.168.0.20", null);
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Disconnect("192.168.0.20");
        var snapshot = tracker.Snapshot();
        await Assert.That(snapshot[0].Status).IsEqualTo(RemoteDeviceStatus.Disconnected);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Disconnect_UnknownDevice_NoOp()
    {
        var tracker = new RemoteDeviceTracker();
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Disconnect("10.0.0.99");
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Disconnect_AlreadyDisconnected_DoesNotRaiseAgain()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.30", null);
        tracker.Disconnect("10.0.0.30");
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Disconnect("10.0.0.30");
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Disconnect_EmptyAddress_NoOp()
    {
        var tracker = new RemoteDeviceTracker();
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Disconnect(string.Empty);
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Forget_KnownDevice_RemovesAndRaisesChanged()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.40", null);
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Forget("10.0.0.40");
        await Assert.That(tracker.Snapshot().Count).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Forget_UnknownDevice_NoOp()
    {
        var tracker = new RemoteDeviceTracker();
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Forget("10.0.0.99");
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Forget_EmptyAddress_NoOp()
    {
        var tracker = new RemoteDeviceTracker();
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Forget(string.Empty);
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Rename_KnownDevice_UpdatesLabelAndRaisesChanged()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.50", null);
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Rename("10.0.0.50", "My Phone");
        var snapshot = tracker.Snapshot();
        await Assert.That(snapshot[0].Name).IsEqualTo("My Phone");
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Rename_UnknownDevice_NoOp()
    {
        var tracker = new RemoteDeviceTracker();
        var count = 0;
        tracker.Changed += _ => count++;
        tracker.Rename("10.0.0.99", "Ghost");
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Rename_EmptyName_Throws()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.51", null);
        await Assert
            .That(() => tracker.Rename("10.0.0.51", string.Empty))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Tick_ActiveDeviceBeyondIdleThreshold_MarksIdle()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var tracker = new RemoteDeviceTracker(time, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
        tracker.RecordRequest("10.0.0.60", null);
        time.Advance(TimeSpan.FromSeconds(11));
        tracker.Tick();
        var snapshot = tracker.Snapshot();
        await Assert.That(snapshot[0].Status).IsEqualTo(RemoteDeviceStatus.Idle);
    }

    [Test]
    public async Task Tick_IdleDeviceBeyondDisconnectThreshold_MarksDisconnected()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var tracker = new RemoteDeviceTracker(time, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
        tracker.RecordRequest("10.0.0.70", null);
        time.Advance(TimeSpan.FromSeconds(11));
        tracker.Tick();
        time.Advance(TimeSpan.FromSeconds(15));
        tracker.Tick();
        var snapshot = tracker.Snapshot();
        await Assert.That(snapshot[0].Status).IsEqualTo(RemoteDeviceStatus.Disconnected);
    }

    [Test]
    public async Task Tick_ActiveDeviceWithinThreshold_DoesNotChange()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var tracker = new RemoteDeviceTracker(time, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
        tracker.RecordRequest("10.0.0.80", null);
        var count = 0;
        tracker.Changed += _ => count++;
        time.Advance(TimeSpan.FromSeconds(5));
        tracker.Tick();
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_NoArguments_UsesDefaultThresholds()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.90", null);
        await Assert.That(tracker.Snapshot().Count).IsEqualTo(1);
    }

    [Test]
    public async Task Snapshot_AfterLaterMutation_IsStableAndDecoupled()
    {
        var tracker = new RemoteDeviceTracker();
        tracker.RecordRequest("10.0.0.100", "curl/8.0");
        var beforeRename = tracker.Snapshot();

        tracker.Rename("10.0.0.100", "Renamed");
        tracker.RecordRequest("10.0.0.100", "iOS");
        tracker.Disconnect("10.0.0.100");

        await Assert.That(beforeRename[0].Name).IsEqualTo("10.0.0.100");
        await Assert.That(beforeRename[0].UserAgent).IsEqualTo("curl/8.0");
        await Assert.That(beforeRename[0].RequestCount).IsEqualTo(0L);
        await Assert.That(beforeRename[0].Status).IsEqualTo(RemoteDeviceStatus.Active);

        var after = tracker.Snapshot();
        await Assert.That(after[0].Name).IsEqualTo("Renamed");
        await Assert.That(after[0].UserAgent).IsEqualTo("iOS");
        await Assert.That(after[0].RequestCount).IsEqualTo(1L);
        await Assert.That(after[0].Status).IsEqualTo(RemoteDeviceStatus.Disconnected);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset start)
        {
            _now = start;
        }

        public void Advance(TimeSpan delta)
        {
            _now = _now.Add(delta);
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
