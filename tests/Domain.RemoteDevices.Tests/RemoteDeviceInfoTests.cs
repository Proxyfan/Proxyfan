using System;
using System.Threading.Tasks;
using Proxyfan.Domain.RemoteDevices;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Domain.RemoteDevices.Tests;

public sealed class RemoteDeviceInfoTests
{
    [Test]
    public async Task Constructor_EmptyAddress_Throws()
    {
        await Assert
            .That(() => new RemoteDeviceInfo(string.Empty, DateTimeOffset.UtcNow, null))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Constructor_ValidArguments_InitialisesProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var info = new RemoteDeviceInfo("192.168.0.10", now, "Mozilla/5.0 (iPhone)");
        await Assert.That(info.Address).IsEqualTo("192.168.0.10");
        await Assert.That(info.FirstSeen).IsEqualTo(now);
        await Assert.That(info.LastSeen).IsEqualTo(now);
        await Assert.That(info.Name).IsEqualTo("192.168.0.10");
        await Assert.That(info.UserAgent).IsEqualTo("Mozilla/5.0 (iPhone)");
        await Assert.That(info.Kind).IsEqualTo(RemoteDeviceKind.Ios);
        await Assert.That(info.RequestCount).IsEqualTo(1L);
        await Assert.That(info.Status).IsEqualTo(RemoteDeviceStatus.Active);
    }

    [Test]
    public async Task Constructor_NullUserAgent_KindIsUnknown()
    {
        var info = new RemoteDeviceInfo("10.0.0.1", DateTimeOffset.UtcNow, null);
        await Assert.That(info.Kind).IsEqualTo(RemoteDeviceKind.Unknown);
        await Assert.That(info.UserAgent).IsNull();
    }

    [Test]
    public async Task RecordRequest_RepeatedCalls_UpdatesLastSeenAndIncrements()
    {
        var start = DateTimeOffset.UtcNow;
        var info = new RemoteDeviceInfo("10.0.0.2", start, "curl/8.0");
        info.RecordRequest(start.AddSeconds(1), "curl/8.0");
        info.RecordRequest(start.AddSeconds(2), "curl/8.0");
        await Assert.That(info.RequestCount).IsEqualTo(3L);
        await Assert.That(info.LastSeen).IsEqualTo(start.AddSeconds(2));
    }

    [Test]
    public async Task RecordRequest_NewUserAgent_UpdatesUserAgentAndKind()
    {
        var info = new RemoteDeviceInfo("10.0.0.3", DateTimeOffset.UtcNow, null);
        info.RecordRequest(DateTimeOffset.UtcNow, "Mozilla/5.0 (Linux; Android 14)");
        await Assert.That(info.UserAgent).IsEqualTo("Mozilla/5.0 (Linux; Android 14)");
        await Assert.That(info.Kind).IsEqualTo(RemoteDeviceKind.Android);
    }

    [Test]
    public async Task RecordRequest_SameUserAgent_DoesNotReclassify()
    {
        var info = new RemoteDeviceInfo("10.0.0.4", DateTimeOffset.UtcNow, "Mozilla/5.0 (Windows NT 10.0)");
        info.RecordRequest(DateTimeOffset.UtcNow, "Mozilla/5.0 (Windows NT 10.0)");
        await Assert.That(info.Kind).IsEqualTo(RemoteDeviceKind.Windows);
    }

    [Test]
    public async Task RecordRequest_FromIdle_RestoresActiveStatus()
    {
        var info = new RemoteDeviceInfo("10.0.0.5", DateTimeOffset.UtcNow, null);
        info.MarkIdle();
        await Assert.That(info.Status).IsEqualTo(RemoteDeviceStatus.Idle);
        info.RecordRequest(DateTimeOffset.UtcNow.AddSeconds(5), null);
        await Assert.That(info.Status).IsEqualTo(RemoteDeviceStatus.Active);
    }

    [Test]
    public async Task MarkIdle_FromActive_Transitions()
    {
        var info = new RemoteDeviceInfo("10.0.0.6", DateTimeOffset.UtcNow, null);
        info.MarkIdle();
        await Assert.That(info.Status).IsEqualTo(RemoteDeviceStatus.Idle);
    }

    [Test]
    public async Task MarkIdle_FromDisconnected_DoesNotTransition()
    {
        var info = new RemoteDeviceInfo("10.0.0.7", DateTimeOffset.UtcNow, null);
        info.MarkDisconnected();
        info.MarkIdle();
        await Assert.That(info.Status).IsEqualTo(RemoteDeviceStatus.Disconnected);
    }

    [Test]
    public async Task MarkDisconnected_Always_SetsStatus()
    {
        var info = new RemoteDeviceInfo("10.0.0.8", DateTimeOffset.UtcNow, null);
        info.MarkDisconnected();
        await Assert.That(info.Status).IsEqualTo(RemoteDeviceStatus.Disconnected);
    }

    [Test]
    public async Task Rename_ValidName_UpdatesName()
    {
        var info = new RemoteDeviceInfo("10.0.0.9", DateTimeOffset.UtcNow, null);
        info.Rename("Test Device");
        await Assert.That(info.Name).IsEqualTo("Test Device");
    }

    [Test]
    public async Task Rename_EmptyName_Throws()
    {
        var info = new RemoteDeviceInfo("10.0.0.10", DateTimeOffset.UtcNow, null);
        await Assert
            .That(() => info.Rename(string.Empty))
            .Throws<ArgumentException>();
    }
}
