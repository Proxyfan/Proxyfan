using System.Globalization;
using System.Text;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Formats <see cref="FlowTimings" /> as a human-readable text block listing each
///     captured milestone with ISO-8601 UTC timestamps and computed phase durations.
/// </summary>
public static class FlowTimingFormatter
{
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <summary>
    ///     Renders the timings as a key/value text block. Returns an empty string when no
    ///     milestones have been recorded.
    /// </summary>
    /// <param name="timings">The flow timings to format.</param>
    /// <returns>The formatted timing block.</returns>
    public static string Format(FlowTimings timings)
    {
        if (timings is null)
        {
            return string.Empty;
        }

        if (!HasAnyTimestamp(timings))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendMilestone(builder, "Request started", timings.RequestStartedAt);
        AppendMilestone(builder, "Request completed", timings.RequestCompletedAt);
        AppendMilestone(builder, "Response started", timings.ResponseStartedAt);
        AppendMilestone(builder, "Response completed", timings.ResponseCompletedAt);

        if (timings.RequestStartedAt.HasValue && timings.RequestCompletedAt.HasValue)
        {
            var requestDuration = timings.RequestCompletedAt.Value - timings.RequestStartedAt.Value;
            AppendDuration(builder, "Request duration", requestDuration.TotalMilliseconds);
        }

        if (timings.ResponseStartedAt.HasValue && timings.ResponseCompletedAt.HasValue)
        {
            var responseDuration = timings.ResponseCompletedAt.Value - timings.ResponseStartedAt.Value;
            AppendDuration(builder, "Response duration", responseDuration.TotalMilliseconds);
        }

        if (timings.RequestCompletedAt.HasValue && timings.ResponseStartedAt.HasValue)
        {
            var waiting = timings.ResponseStartedAt.Value - timings.RequestCompletedAt.Value;
            AppendDuration(builder, "Waiting (TTFB)", waiting.TotalMilliseconds);
        }

        if (timings.TotalDuration.HasValue)
        {
            AppendDuration(builder, "Total", timings.TotalDuration.Value.TotalMilliseconds);
        }

        return builder.ToString();
    }

    private static void AppendDuration(StringBuilder builder, string label, double milliseconds)
    {
        builder.Append(label);
        builder.Append(": ");
        builder.Append(milliseconds.ToString("F2", CultureInfo.InvariantCulture));
        builder.AppendLine(" ms");
    }

    private static void AppendMilestone(StringBuilder builder, string label, System.DateTimeOffset? milestone)
    {
        if (!milestone.HasValue)
        {
            return;
        }

        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(milestone.Value.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture));
    }

    private static bool HasAnyTimestamp(FlowTimings timings)
    {
        if (timings.RequestStartedAt.HasValue)
        {
            return true;
        }

        if (timings.RequestCompletedAt.HasValue)
        {
            return true;
        }

        if (timings.ResponseStartedAt.HasValue)
        {
            return true;
        }

        if (timings.ResponseCompletedAt.HasValue)
        {
            return true;
        }

        return false;
    }
}
