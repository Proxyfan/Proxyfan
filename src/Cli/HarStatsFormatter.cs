using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Proxyfan.Cli;

/// <summary>
///     Formats aggregated metrics over a HAR document into a human-readable text report.
///     Used by the <c>har-stats</c> CLI command to give CI / ops a quick at-a-glance summary
///     of a capture without scrolling through hundreds of individual flow lines.
/// </summary>
public static class HarStatsFormatter
{
    /// <summary>
    ///     Builds the multi-line text report from the supplied flows.
    /// </summary>
    /// <param name="flows">The flows to summarise.</param>
    /// <returns>The text report.</returns>
    public static string BuildReport(IReadOnlyList<TrafficFlow> flows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Total flows: {flows.Count}");

        if (flows.Count == 0)
        {
            return builder.ToString();
        }

        AppendStatusDistribution(builder, flows);
        AppendMethodDistribution(builder, flows);
        AppendByteTotals(builder, flows);
        AppendDurationStats(builder, flows);
        return builder.ToString();
    }

    private static void AppendByteTotals(StringBuilder builder, IReadOnlyList<TrafficFlow> flows)
    {
        long requestBytes = 0;
        long responseBytes = 0;

        foreach (var flow in flows)
        {
            requestBytes += flow.Request?.Body.Length ?? 0;
            responseBytes += flow.Response?.Body.Length ?? 0;
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"Request body bytes:  {requestBytes:N0}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Response body bytes: {responseBytes:N0}");
    }

    private static void AppendDurationStats(StringBuilder builder, IReadOnlyList<TrafficFlow> flows)
    {
        var durations = new List<double>(flows.Count);

        foreach (var flow in flows)
        {
            if (flow.Timings.RequestStartedAt.HasValue && flow.Timings.RequestCompletedAt.HasValue)
            {
                durations.Add((flow.Timings.RequestCompletedAt.Value - flow.Timings.RequestStartedAt.Value).TotalMilliseconds);
            }
        }

        if (durations.Count == 0)
        {
            return;
        }

        durations.Sort();
        var min = durations[0];
        var max = durations[^1];
        var medianIndex = durations.Count / 2;
        var median = durations[medianIndex];
        builder.AppendLine(CultureInfo.InvariantCulture, $"Duration (ms): min={min:F0} median={median:F0} max={max:F0} samples={durations.Count}");
    }

    private static void AppendMethodDistribution(StringBuilder builder, IReadOnlyList<TrafficFlow> flows)
    {
        var byMethod = new SortedDictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var flow in flows)
        {
            var method = flow.Request?.Method ?? "-";

            if (byMethod.TryGetValue(method, out var existing))
            {
                byMethod[method] = existing + 1;
            }
            else
            {
                byMethod[method] = 1;
            }
        }

        builder.AppendLine("Methods:");

        foreach (var entry in byMethod)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {entry.Key,-8} {entry.Value}");
        }
    }

    private static void AppendStatusDistribution(StringBuilder builder, IReadOnlyList<TrafficFlow> flows)
    {
        var byClass = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        foreach (var flow in flows)
        {
            var classification = ClassifyStatus(flow);

            if (byClass.TryGetValue(classification, out var existing))
            {
                byClass[classification] = existing + 1;
            }
            else
            {
                byClass[classification] = 1;
            }
        }

        builder.AppendLine("Status classes:");

        foreach (var entry in byClass)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {entry.Key,-5} {entry.Value}");
        }
    }

    private static string ClassifyStatus(TrafficFlow flow)
    {
        if (flow.Response is null)
        {
            return "(no response)";
        }

        var statusCode = flow.Response.StatusCode;

        if (statusCode is >= 200 and < 300)
        {
            return "2xx";
        }

        if (statusCode is >= 300 and < 400)
        {
            return "3xx";
        }

        if (statusCode is >= 400 and < 500)
        {
            return "4xx";
        }

        if (statusCode is >= 500 and < 600)
        {
            return "5xx";
        }

        return "other";
    }
}
