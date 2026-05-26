using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="FlowTimings" />.
/// </summary>
public sealed class FlowTimingsTests
{
    /// <summary>
    ///     Verifies that <see cref="FlowTimings.TotalDuration" /> returns null when the request
    ///     has not yet started.
    /// </summary>
    [Test]
    public async Task TotalDuration_WhenRequestNotStarted_IsNull()
    {
        var timings = new FlowTimings(null, null, null, null);

        await Assert.That(timings.TotalDuration).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="FlowTimings.TotalDuration" /> returns null when the response
    ///     has not yet completed.
    /// </summary>
    [Test]
    public async Task TotalDuration_WhenResponseNotCompleted_IsNull()
    {
        var requestStartedAt = DateTimeOffset.UtcNow;
        var timings = new FlowTimings(requestStartedAt, null, null, null);

        await Assert.That(timings.TotalDuration).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="FlowTimings.TotalDuration" /> returns the correct span
    ///     when both the request start and response completion times are known.
    /// </summary>
    [Test]
    public async Task TotalDuration_WhenBothTimestampsPresent_ReturnsCorrectSpan()
    {
        var requestStartedAt = DateTimeOffset.UtcNow;
        var responseCompletedAt = requestStartedAt.AddSeconds(2);
        var timings = new FlowTimings(requestStartedAt, null, null, responseCompletedAt);

        await Assert.That(timings.TotalDuration).IsNotNull();
        await Assert.That(timings.TotalDuration!.Value.TotalSeconds).IsEqualTo(2.0);
    }

    /// <summary>
    ///     Verifies that all timing properties are stored correctly via the constructor.
    /// </summary>
    [Test]
    public async Task Constructor_WithAllValues_StoresAllTimestamps()
    {
        var requestStartedAt = DateTimeOffset.UtcNow;
        var requestCompletedAt = requestStartedAt.AddMilliseconds(100);
        var responseStartedAt = requestCompletedAt.AddMilliseconds(50);
        var responseCompletedAt = responseStartedAt.AddMilliseconds(200);

        var timings = new FlowTimings(requestStartedAt, requestCompletedAt, responseStartedAt, responseCompletedAt);

        await Assert.That(timings.RequestStartedAt).IsEqualTo(requestStartedAt);
        await Assert.That(timings.RequestCompletedAt).IsEqualTo(requestCompletedAt);
        await Assert.That(timings.ResponseStartedAt).IsEqualTo(responseStartedAt);
        await Assert.That(timings.ResponseCompletedAt).IsEqualTo(responseCompletedAt);
    }
}