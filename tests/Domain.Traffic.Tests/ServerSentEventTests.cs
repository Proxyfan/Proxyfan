using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEvent" />.
/// </summary>
public sealed class ServerSentEventTests
{
    /// <summary>
    ///     Verifies that the constructor stores all supplied fields.
    /// </summary>
    [Test]
    public async Task Constructor_GivenAllFields_StoresAllValues()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var sse = new ServerSentEvent("data", "update", "42", 5000, timestamp);

        await Assert.That(sse.Data).IsEqualTo("data");
        await Assert.That(sse.EventType).IsEqualTo("update");
        await Assert.That(sse.Id).IsEqualTo("42");
        await Assert.That(sse.RetryMilliseconds).IsEqualTo(5000);
        await Assert.That(sse.Timestamp).IsEqualTo(timestamp);
    }

    /// <summary>
    ///     Verifies that the optional fields can be null.
    /// </summary>
    [Test]
    public async Task Constructor_WithMinimalFields_AcceptsNullOptional()
    {
        var sse = new ServerSentEvent(string.Empty, null, null, null, DateTimeOffset.UtcNow);

        await Assert.That(sse.EventType).IsNull();
        await Assert.That(sse.Id).IsNull();
        await Assert.That(sse.RetryMilliseconds).IsNull();
    }
}
