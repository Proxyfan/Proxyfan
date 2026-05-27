using System;
using System.Threading.Tasks;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="FlowTimingFormatter" />.
/// </summary>
public sealed class FlowTimingFormatterTests
{
    /// <summary>
    ///     Verifies that empty timings produce an empty string.
    /// </summary>
    [Test]
    public async Task Format_NoTimestamps_ReturnsEmpty()
    {
        var timings = new FlowTimings(null, null, null, null);

        var result = FlowTimingFormatter.Format(timings);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a null input produces an empty string.
    /// </summary>
    [Test]
    public async Task Format_NullInput_ReturnsEmpty()
    {
        var result = FlowTimingFormatter.Format(null!);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that a single milestone yields a labeled line.
    /// </summary>
    [Test]
    public async Task Format_OnlyRequestStarted_ContainsLabel()
    {
        var moment = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timings = new FlowTimings(moment, null, null, null);

        var result = FlowTimingFormatter.Format(timings);

        await Assert.That(result.Contains("Request started", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("2024-01-01", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that all four milestones plus computed durations appear when present.
    /// </summary>
    [Test]
    public async Task Format_FullTimeline_IncludesDerivedDurations()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timings = new FlowTimings(
            start,
            start.AddMilliseconds(10),
            start.AddMilliseconds(50),
            start.AddMilliseconds(200));

        var result = FlowTimingFormatter.Format(timings);

        await Assert.That(result.Contains("Request started", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Request completed", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Response started", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Response completed", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Request duration:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Response duration:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Waiting (TTFB):", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains("Total:", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.Contains(" ms", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that the response-only timing milestone is emitted.
    /// </summary>
    [Test]
    public async Task Format_OnlyResponseCompleted_ContainsLabel()
    {
        var moment = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timings = new FlowTimings(null, null, null, moment);

        var result = FlowTimingFormatter.Format(timings);

        await Assert.That(result.Contains("Response completed", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that the request-completed-only milestone is emitted.
    /// </summary>
    [Test]
    public async Task Format_OnlyRequestCompleted_ContainsLabel()
    {
        var moment = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var timings = new FlowTimings(null, moment, null, null);

        var result = FlowTimingFormatter.Format(timings);

        await Assert.That(result.Contains("Request completed", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that the response-started-only milestone is emitted.
    /// </summary>
    [Test]
    public async Task Format_OnlyResponseStarted_ContainsLabel()
    {
        var moment = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var timings = new FlowTimings(null, null, moment, null);

        var result = FlowTimingFormatter.Format(timings);

        await Assert.That(result.Contains("Response started", StringComparison.Ordinal)).IsTrue();
    }
}
