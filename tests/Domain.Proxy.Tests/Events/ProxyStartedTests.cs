using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy.Events;

namespace Proxyfan.Domain.Proxy.Tests.Events;

/// <summary>
///     Tests for <see cref="ProxyStarted" />.
/// </summary>
public sealed class ProxyStartedTests
{
    /// <summary>
    ///     Verifies record equality for <see cref="ProxyStarted" />.
    /// </summary>
    [Test]
    public async Task Equality_SameValues_AreEqual()
    {
        var ts = DateTimeOffset.UtcNow;
        var a = new ProxyStarted(8080, ts);
        var b = new ProxyStarted(8080, ts);
        await Assert.That(a).IsEqualTo(b);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyStarted.Port" /> is set from the constructor.
    /// </summary>
    [Test]
    public async Task Port_WhenConstructed_ReturnsProvidedPort()
    {
        var evt = new ProxyStarted(8080, DateTimeOffset.UtcNow);
        await Assert.That(evt.Port).IsEqualTo(8080);
    }

    /// <summary>
    ///     Verifies that <see cref="ProxyStarted.Timestamp" /> is set from the constructor.
    /// </summary>
    [Test]
    public async Task Timestamp_WhenConstructed_ReturnsProvidedTimestamp()
    {
        var ts = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var evt = new ProxyStarted(8080, ts);
        await Assert.That(evt.Timestamp).IsEqualTo(ts);
    }
}