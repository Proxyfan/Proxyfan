using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Domain.Updates;

namespace Proxyfan.Domain.Updates.Tests;

/// <summary>
///     Tests for <see cref="MutableUpdateNotification" />.
/// </summary>
public sealed class MutableUpdateNotificationTests
{
    /// <summary>
    ///     Verifies a newly constructed notification reports no current update.
    /// </summary>
    [Test]
    public async Task Latest_InitialState_IsNull()
    {
        var notification = new MutableUpdateNotification();

        await Assert.That(notification.Latest).IsNull();
    }

    /// <summary>
    ///     Verifies publishing a value updates Latest and raises the Changed event.
    /// </summary>
    [Test]
    public async Task Publish_NewUpdate_RaisesChangedAndUpdatesLatest()
    {
        var notification = new MutableUpdateNotification();
        var received = new List<UpdateInfo?>();
        notification.Changed += value => received.Add(value);

        var update = CreateUpdate("2.0.0");
        notification.Publish(update);

        await Assert.That(notification.Latest).IsSameReferenceAs(update);
        await Assert.That(received.Count).IsEqualTo(1);
        await Assert.That(received[0]).IsSameReferenceAs(update);
    }

    /// <summary>
    ///     Verifies publishing the same version twice does not raise Changed the second time.
    /// </summary>
    [Test]
    public async Task Publish_SameVersionTwice_RaisesChangedOnce()
    {
        var notification = new MutableUpdateNotification();
        var changeCount = 0;
        notification.Changed += _ => changeCount++;

        notification.Publish(CreateUpdate("2.0.0"));
        notification.Publish(CreateUpdate("2.0.0"));

        await Assert.That(changeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies publishing a different version raises Changed each time.
    /// </summary>
    [Test]
    public async Task Publish_DifferentVersions_RaisesChangedForEach()
    {
        var notification = new MutableUpdateNotification();
        var changeCount = 0;
        notification.Changed += _ => changeCount++;

        notification.Publish(CreateUpdate("2.0.0"));
        notification.Publish(CreateUpdate("2.1.0"));
        notification.Publish(CreateUpdate("3.0.0"));

        await Assert.That(changeCount).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies Clear after a publish raises Changed with null.
    /// </summary>
    [Test]
    public async Task Clear_AfterPublish_RaisesChangedWithNull()
    {
        var notification = new MutableUpdateNotification();
        notification.Publish(CreateUpdate("2.0.0"));
        UpdateInfo? received = CreateUpdate("dummy");
        var changeCount = 0;
        notification.Changed += value =>
        {
            received = value;
            changeCount++;
        };

        notification.Clear();

        await Assert.That(notification.Latest).IsNull();
        await Assert.That(changeCount).IsEqualTo(1);
        await Assert.That(received).IsNull();
    }

    /// <summary>
    ///     Verifies Clear from initial null state does not raise Changed.
    /// </summary>
    [Test]
    public async Task Clear_FromNullState_DoesNotRaiseChanged()
    {
        var notification = new MutableUpdateNotification();
        var changeCount = 0;
        notification.Changed += _ => changeCount++;

        notification.Clear();

        await Assert.That(changeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies Publish(null) when no prior value is set does not raise Changed.
    /// </summary>
    [Test]
    public async Task Publish_NullFromNullState_DoesNotRaiseChanged()
    {
        var notification = new MutableUpdateNotification();
        var changeCount = 0;
        notification.Changed += _ => changeCount++;

        notification.Publish(null);

        await Assert.That(changeCount).IsEqualTo(0);
    }

    private static UpdateInfo CreateUpdate(string version)
    {
        return new UpdateInfo
        {
            Version = version,
            DownloadUrl = "https://example.com/release",
            ReleaseNotes = "notes",
        };
    }
}
