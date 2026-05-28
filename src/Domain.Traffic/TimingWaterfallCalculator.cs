using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Computes a sequence of <see cref="TimingPhase" /> segments suitable for a waterfall
///     visualization from the timing milestones captured on a <see cref="TrafficFlow" />.
/// </summary>
public static class TimingWaterfallCalculator
{
    /// <summary>
    ///     Calculates the waterfall phases. Returns an empty list when there is no measurable
    ///     duration (no milestones, or all milestones collapse to the same instant).
    /// </summary>
    /// <param name="timings">The flow timing milestones to project onto the waterfall.</param>
    /// <returns>An ordered, non-empty list of phases or an empty list when no data is available.</returns>
    public static IReadOnlyList<TimingPhase> Calculate(FlowTimings? timings)
    {
        if (timings is null)
        {
            return [];
        }

        var earliest = FindEarliest(timings);
        var latest = FindLatest(timings);

        if (!earliest.HasValue)
        {
            return [];
        }

        if (!latest.HasValue)
        {
            return [];
        }

        var total = latest.Value - earliest.Value;

        if (total <= TimeSpan.Zero)
        {
            return [];
        }

        var phases = new List<TimingPhase>(capacity: 3);
        var window = new TimingWindow
        {
            Origin = earliest.Value,
            TotalMilliseconds = total.TotalMilliseconds,
        };
        var requestRange = new PhaseRange
        {
            Name = "Request",
            Start = timings.RequestStartedAt,
            End = timings.RequestCompletedAt,
        };
        var waitingRange = new PhaseRange
        {
            Name = "Waiting",
            Start = timings.RequestCompletedAt,
            End = timings.ResponseStartedAt,
        };
        var responseRange = new PhaseRange
        {
            Name = "Response",
            Start = timings.ResponseStartedAt,
            End = timings.ResponseCompletedAt,
        };
        AppendPhase(phases, window, requestRange);
        AppendPhase(phases, window, waitingRange);
        AppendPhase(phases, window, responseRange);
        return phases;
    }

    private static void AppendPhase(List<TimingPhase> phases, TimingWindow window, PhaseRange range)
    {
        if (!range.Start.HasValue)
        {
            return;
        }

        if (!range.End.HasValue)
        {
            return;
        }

        var phaseDuration = range.End.Value - range.Start.Value;

        if (phaseDuration <= TimeSpan.Zero)
        {
            return;
        }

        var startFraction = (range.Start.Value - window.Origin).TotalMilliseconds / window.TotalMilliseconds;
        var widthFraction = phaseDuration.TotalMilliseconds / window.TotalMilliseconds;
        var clampedStart = Math.Clamp(startFraction, 0d, 1d);
        var clampedWidth = Math.Clamp(widthFraction, 0d, 1d - clampedStart);
        var phase = new TimingPhase(range.Name, clampedStart, clampedWidth, phaseDuration.TotalMilliseconds);
        phases.Add(phase);
    }

    private static DateTimeOffset? Earlier(DateTimeOffset? current, DateTimeOffset? candidate)
    {
        if (!candidate.HasValue)
        {
            return current;
        }

        if (!current.HasValue)
        {
            return candidate;
        }

        if (candidate.Value < current.Value)
        {
            return candidate;
        }

        return current;
    }

    private static DateTimeOffset? FindEarliest(FlowTimings timings)
    {
        DateTimeOffset? result = null;
        result = Earlier(result, timings.RequestStartedAt);
        result = Earlier(result, timings.RequestCompletedAt);
        result = Earlier(result, timings.ResponseStartedAt);
        result = Earlier(result, timings.ResponseCompletedAt);
        return result;
    }

    private static DateTimeOffset? FindLatest(FlowTimings timings)
    {
        DateTimeOffset? result = null;
        result = Later(result, timings.RequestStartedAt);
        result = Later(result, timings.RequestCompletedAt);
        result = Later(result, timings.ResponseStartedAt);
        result = Later(result, timings.ResponseCompletedAt);
        return result;
    }

    private static DateTimeOffset? Later(DateTimeOffset? current, DateTimeOffset? candidate)
    {
        if (!candidate.HasValue)
        {
            return current;
        }

        if (!current.HasValue)
        {
            return candidate;
        }

        if (candidate.Value > current.Value)
        {
            return candidate;
        }

        return current;
    }

    private readonly struct PhaseRange
    {
        public DateTimeOffset? End { get; init; }

        public required string Name { get; init; }

        public DateTimeOffset? Start { get; init; }
    }

    private readonly struct TimingWindow
    {
        public DateTimeOffset Origin { get; init; }

        public double TotalMilliseconds { get; init; }
    }
}
