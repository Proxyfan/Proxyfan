using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy.Events;

namespace Proxyfan.Domain.Proxy.Tests.Events;

/// <summary>Tests for <see cref="ProxyErrorOccurred" />.</summary>
internal sealed class ProxyErrorOccurredTests
{
    /// <summary>Verifies that <see cref="ProxyErrorOccurred.Error" /> is set from the constructor.</summary>
    [Test]
    public async Task Error_WhenConstructed_ReturnsProvidedError()
    {
        var error = new ProxyAlreadyRunningError();
        var evt = new ProxyErrorOccurred(error, DateTimeOffset.UtcNow);
        await Assert.That(evt.Error).IsSameReferenceAs(error);
    }

    /// <summary>Verifies that <see cref="ProxyErrorOccurred.Timestamp" /> is set from the constructor.</summary>
    [Test]
    public async Task Timestamp_WhenConstructed_ReturnsProvidedTimestamp()
    {
        var ts = new DateTimeOffset(2025, 3, 10, 9, 0, 0, TimeSpan.Zero);
        var evt = new ProxyErrorOccurred(new ProxyNotRunningError(), ts);
        await Assert.That(evt.Timestamp).IsEqualTo(ts);
    }
}
