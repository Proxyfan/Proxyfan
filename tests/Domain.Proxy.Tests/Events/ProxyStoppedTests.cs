using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy.Events;

namespace Proxyfan.Domain.Proxy.Tests.Events;

/// <summary>
///     Tests for <see cref="ProxyStopped" />.
/// </summary>
public sealed class ProxyStoppedTests
{
    /// <summary>
    ///     Verifies record equality for <see cref="ProxyStopped" />.
    /// </summary>
    [Test]
    public async Task Equality_SameValues_AreEqual()
    {
        var ts = DateTimeOffset.UtcNow;
        var a = new ProxyStopped(ts);
        var b = new ProxyStopped(ts);
        await Assert.That(a).IsEqualTo(b);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyStopped.Timestamp" /> is set from the constructor.
    /// </summary>
    [Test]
    public async Task Timestamp_WhenConstructed_ReturnsProvidedTimestamp()
    {
        var ts = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var evt = new ProxyStopped(ts);
        await Assert.That(evt.Timestamp).IsEqualTo(ts);
    }
}