using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="TimingWaterfallCalculator" />.
/// </summary>
public sealed class TimingWaterfallCalculatorTests
{
    /// <summary>
    ///     Verifies that the calculator returns an empty list when timings is null.
    /// </summary>
    [Test]
    public async Task Calculate_NullTimings_ReturnsEmpty()
    {
        var phases = TimingWaterfallCalculator.Calculate(null);

        await Assert.That(phases).IsNotNull();
        await Assert.That(phases.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the calculator returns an empty list when all milestones are null.
    /// </summary>
    [Test]
    public async Task Calculate_AllMilestonesNull_ReturnsEmpty()
    {
        var timings = new FlowTimings(null, null, null, null);

        var phases = TimingWaterfallCalculator.Calculate(timings);

        await Assert.That(phases.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the calculator returns an empty list when only one milestone is set
    ///     (no measurable total duration).
    /// </summary>
    [Test]
    public async Task Calculate_OnlyOneMilestone_ReturnsEmpty()
    {
        var now = DateTimeOffset.UtcNow;
        var timings = new FlowTimings(now, null, null, null);

        var phases = TimingWaterfallCalculator.Calculate(timings);

        await Assert.That(phases.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the calculator returns an empty list when all milestones collapse
    ///     to the same instant.
    /// </summary>
    [Test]
    public async Task Calculate_AllMilestonesEqual_ReturnsEmpty()
    {
        var now = DateTimeOffset.UtcNow;
        var timings = new FlowTimings(now, now, now, now);

        var phases = TimingWaterfallCalculator.Calculate(timings);

        await Assert.That(phases.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a full set of milestones produces Request/Waiting/Response phases
    ///     with correct fractional offsets relative to the earliest milestone.
    /// </summary>
    [Test]
    public async Task Calculate_FullMilestoneSet_ProducesThreePhasesInOrder()
    {
        var origin = DateTimeOffset.UtcNow;
        var requestStart = origin;
        var requestEnd = origin.AddMilliseconds(100);
        var responseStart = origin.AddMilliseconds(300);
        var responseEnd = origin.AddMilliseconds(500);
        var timings = new FlowTimings(requestStart, requestEnd, responseStart, responseEnd);

        var phases = TimingWaterfallCalculator.Calculate(timings);

        await Assert.That(phases.Count).IsEqualTo(3);
        await Assert.That(phases[0].Name).IsEqualTo("Request");
        await Assert.That(phases[1].Name).IsEqualTo("Waiting");
        await Assert.That(phases[2].Name).IsEqualTo("Response");
        await Assert.That(phases[0].StartFraction).IsEqualTo(0d);
        await Assert.That(phases[0].WidthFraction).IsEqualTo(100d / 500d);
        await Assert.That(phases[1].StartFraction).IsEqualTo(100d / 500d);
        await Assert.That(phases[1].WidthFraction).IsEqualTo(200d / 500d);
        await Assert.That(phases[2].StartFraction).IsEqualTo(300d / 500d);
        await Assert.That(phases[2].WidthFraction).IsEqualTo(200d / 500d);
        await Assert.That(phases[0].DurationMilliseconds).IsEqualTo(100d);
        await Assert.That(phases[1].DurationMilliseconds).IsEqualTo(200d);
        await Assert.That(phases[2].DurationMilliseconds).IsEqualTo(200d);
    }

    /// <summary>
    ///     Verifies that only the Request phase is produced when only the request milestones
    ///     are set.
    /// </summary>
    [Test]
    public async Task Calculate_OnlyRequestPhase_ProducesRequestOnly()
    {
        var origin = DateTimeOffset.UtcNow;
        var requestEnd = origin.AddMilliseconds(50);
        var timings = new FlowTimings(origin, requestEnd, null, null);

        var phases = TimingWaterfallCalculator.Calculate(timings);

        await Assert.That(phases.Count).IsEqualTo(1);
        await Assert.That(phases[0].Name).IsEqualTo("Request");
        await Assert.That(phases[0].DurationMilliseconds).IsEqualTo(50d);
    }

    /// <summary>
    ///     Verifies that zero-duration phases are omitted from the result.
    /// </summary>
    [Test]
    public async Task Calculate_ZeroDurationRequestAndWaiting_OmitsThosePhases()
    {
        var origin = DateTimeOffset.UtcNow;
        var responseEnd = origin.AddMilliseconds(100);
        var timings = new FlowTimings(origin, origin, origin, responseEnd);

        var phases = TimingWaterfallCalculator.Calculate(timings);

        await Assert.That(phases.Count).IsEqualTo(1);
        await Assert.That(phases[0].Name).IsEqualTo("Response");
        await Assert.That(phases[0].DurationMilliseconds).IsEqualTo(100d);
    }

    /// <summary>
    ///     Verifies that fractional offsets are clamped to <c>[0, 1]</c>.
    /// </summary>
    [Test]
    public async Task Calculate_FullSet_FractionsAreClamped()
    {
        var origin = DateTimeOffset.UtcNow;
        var timings = new FlowTimings(
            origin,
            origin.AddMilliseconds(10),
            origin.AddMilliseconds(20),
            origin.AddMilliseconds(30));

        var phases = TimingWaterfallCalculator.Calculate(timings);

        foreach (var phase in phases)
        {
            await Assert.That(phase.StartFraction).IsGreaterThanOrEqualTo(0d);
            await Assert.That(phase.StartFraction).IsLessThanOrEqualTo(1d);
            await Assert.That(phase.WidthFraction).IsGreaterThanOrEqualTo(0d);
            await Assert.That(phase.StartFraction + phase.WidthFraction).IsLessThanOrEqualTo(1d);
        }
    }
}
